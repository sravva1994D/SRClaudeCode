using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Users;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Reads and saves the doctor account inside the current unit of work.</summary>
public sealed class SqlUserAccountRepository(
    IAuditLogWriter auditLogWriter,
    IClock clock,
    IOptions<DatabaseOptions> options) : IUserAccountRepository
{
    private const string Columns = """
        UserId, Username, PasswordHash, IsActive, FailedLoginCount, FailedLoginWindowStartDate,
        LockedUntilDate, LastLoginDate, PasswordChangedDate, RowVersion
        """;

    private const string FindByUsernameSql = $"SELECT {Columns} FROM dbo.Users WHERE Username = @Username AND IsActive = 1;";
    private const string FindByIdSql = $"SELECT {Columns} FROM dbo.Users WHERE UserId = @UserId AND IsActive = 1;";

    private const string UpdateSql = """
        UPDATE dbo.Users
        SET PasswordHash = @PasswordHash,
            FailedLoginCount = @FailedLoginCount,
            FailedLoginWindowStartDate = @FailedLoginWindowStartDate,
            LockedUntilDate = @LockedUntilDate,
            LastLoginDate = @LastLoginDate,
            PasswordChangedDate = @PasswordChangedDate,
            ModifiedDate = @ModifiedDate,
            ModifiedBy = @ModifiedBy
        OUTPUT inserted.RowVersion
        WHERE UserId = @UserId AND RowVersion = @RowVersion;
        """;

    public Task<UserAccount?> FindActiveByUsernameAsync(string username, CancellationToken cancellationToken) =>
        FindOneAsync(FindByUsernameSql, new SqlParameter("@Username", username), cancellationToken);

    public Task<UserAccount?> FindActiveByIdAsync(int userId, CancellationToken cancellationToken) =>
        FindOneAsync(FindByIdSql, new SqlParameter("@UserId", userId), cancellationToken);

    public async Task SaveAsync(
        UserAccount account,
        IReadOnlyDictionary<string, object?> auditValuesBefore,
        int? changedByUserId,
        CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        var nowUtc = clock.UtcNow;

        await using var command = CreateCommand(scope, UpdateSql);
        command.Parameters.Add(new SqlParameter("@PasswordHash", account.PasswordHash));
        command.Parameters.Add(new SqlParameter("@FailedLoginCount", account.FailedLoginCount));
        command.Parameters.Add(NullableDate("@FailedLoginWindowStartDate", account.FailedLoginWindowStartUtc));
        command.Parameters.Add(NullableDate("@LockedUntilDate", account.LockedUntilUtc));
        command.Parameters.Add(NullableDate("@LastLoginDate", account.LastLoginUtc));
        command.Parameters.Add(NullableDate("@PasswordChangedDate", account.PasswordChangedUtc));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", nowUtc));
        command.Parameters.Add(new SqlParameter("@ModifiedBy", (object?)changedByUserId ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@UserId", account.UserId));
        command.Parameters.Add(new SqlParameter("@RowVersion", account.RowVersion));

        var newRowVersion = await command.ExecuteScalarAsync(cancellationToken) as byte[]
            ?? throw new ConcurrencyConflictException("Users");
        account.SetRowVersion(newRowVersion);

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                "Users",
                account.UserId.ToString(CultureInfo.InvariantCulture),
                AuditAction.Update,
                auditValuesBefore,
                account.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    private async Task<UserAccount?> FindOneAsync(string sql, SqlParameter parameter, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = CreateCommand(scope, sql);
        command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return UserAccount.Reconstitute(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetBoolean(3),
            reader.GetInt32(4),
            NullableDateTime(reader, 5),
            NullableDateTime(reader, 6),
            NullableDateTime(reader, 7),
            NullableDateTime(reader, 8),
            (byte[])reader.GetValue(9));
    }

    private DbCommand CreateCommand(UnitOfWorkScope scope, string sql)
    {
        var command = scope.Connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        return command;
    }

    private static SqlParameter NullableDate(string name, DateTime? value) =>
        new(name, (object?)value ?? DBNull.Value);

    private static DateTime? NullableDateTime(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
}
