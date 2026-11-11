using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Auth;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Stores refresh tokens inside the current unit of work. Every change writes an audit row.</summary>
public sealed class SqlRefreshTokenRepository(
    IAuditLogWriter auditLogWriter,
    IClock clock,
    IOptions<DatabaseOptions> options) : IRefreshTokenRepository
{
    private const string TableName = "RefreshTokens";

    private const string Columns = """
        RefreshTokenId, UserId, FamilyId, TokenHash, IssuedDate, ExpiresDate, LastUsedDate,
        RevokedDate, RevokedReason, RowVersion
        """;

    private const string FindByHashSql = $"SELECT {Columns} FROM dbo.RefreshTokens WHERE TokenHash = @TokenHash;";

    private const string FindActiveInFamilySql = $"""
        SELECT {Columns} FROM dbo.RefreshTokens
        WHERE FamilyId = @FamilyId AND RevokedDate IS NULL;
        """;

    private const string FindActiveForUserSql = $"""
        SELECT {Columns} FROM dbo.RefreshTokens
        WHERE UserId = @UserId AND RevokedDate IS NULL;
        """;

    private const string InsertSql = """
        INSERT INTO dbo.RefreshTokens
            (UserId, FamilyId, TokenHash, IssuedDate, ExpiresDate, LastUsedDate, CreatedBy, ModifiedBy)
        OUTPUT inserted.RefreshTokenId, inserted.RowVersion
        VALUES (@UserId, @FamilyId, @TokenHash, @IssuedDate, @ExpiresDate, @LastUsedDate, @UserId, @UserId);
        """;

    private const string UpdateSql = """
        UPDATE dbo.RefreshTokens
        SET LastUsedDate = @LastUsedDate,
            RevokedDate = @RevokedDate,
            RevokedReason = @RevokedReason,
            ModifiedDate = @ModifiedDate,
            ModifiedBy = @ModifiedBy
        OUTPUT inserted.RowVersion
        WHERE RefreshTokenId = @RefreshTokenId AND RowVersion = @RowVersion;
        """;

    public async Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var matches = await ReadAsync(FindByHashSql, new SqlParameter("@TokenHash", tokenHash), cancellationToken);
        return matches.SingleOrDefault();
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = CreateCommand(scope, InsertSql);
        command.Parameters.Add(new SqlParameter("@UserId", token.UserId));
        command.Parameters.Add(new SqlParameter("@FamilyId", token.FamilyId));
        command.Parameters.Add(new SqlParameter("@TokenHash", token.TokenHash));
        command.Parameters.Add(new SqlParameter("@IssuedDate", token.IssuedUtc));
        command.Parameters.Add(new SqlParameter("@ExpiresDate", token.ExpiresUtc));
        command.Parameters.Add(new SqlParameter("@LastUsedDate", token.LastUsedUtc));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        token.SetRefreshTokenId(reader.GetInt32(0));
        token.SetRowVersion((byte[])reader.GetValue(1));
        await reader.CloseAsync();

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                token.RefreshTokenId.ToString(CultureInfo.InvariantCulture),
                AuditAction.Create,
                null,
                CreatedValues(token),
                token.UserId),
            cancellationToken);
    }

    public async Task SaveAsync(
        RefreshToken token,
        IReadOnlyDictionary<string, object?> auditValuesBefore,
        CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = CreateCommand(scope, UpdateSql);
        command.Parameters.Add(new SqlParameter("@LastUsedDate", token.LastUsedUtc));
        command.Parameters.Add(new SqlParameter("@RevokedDate", (object?)token.RevokedUtc ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@RevokedReason", (object?)token.RevokedReason ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));
        command.Parameters.Add(new SqlParameter("@ModifiedBy", token.UserId));
        command.Parameters.Add(new SqlParameter("@RefreshTokenId", token.RefreshTokenId));
        command.Parameters.Add(new SqlParameter("@RowVersion", token.RowVersion));

        var newRowVersion = await command.ExecuteScalarAsync(cancellationToken) as byte[]
            ?? throw new ConcurrencyConflictException(TableName);
        token.SetRowVersion(newRowVersion);

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                token.RefreshTokenId.ToString(CultureInfo.InvariantCulture),
                AuditAction.Update,
                auditValuesBefore,
                token.AuditValues(),
                token.UserId),
            cancellationToken);
    }

    public async Task RevokeFamilyAsync(Guid familyId, string reason, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var active = await ReadAsync(FindActiveInFamilySql, new SqlParameter("@FamilyId", familyId), cancellationToken);
        await RevokeAllAsync(active, reason, nowUtc, cancellationToken);
    }

    public async Task RevokeAllForUserAsync(int userId, string reason, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var active = await ReadAsync(FindActiveForUserSql, new SqlParameter("@UserId", userId), cancellationToken);
        await RevokeAllAsync(active, reason, nowUtc, cancellationToken);
    }

    private async Task RevokeAllAsync(
        IReadOnlyList<RefreshToken> tokens,
        string reason,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        foreach (var token in tokens)
        {
            var auditValuesBefore = token.AuditValues();
            token.Revoke(nowUtc, reason);
            await SaveAsync(token, auditValuesBefore, cancellationToken);
        }
    }

    private async Task<List<RefreshToken>> ReadAsync(string sql, SqlParameter parameter, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = CreateCommand(scope, sql);
        command.Parameters.Add(parameter);

        var tokens = new List<RefreshToken>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tokens.Add(RefreshToken.Reconstitute(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetGuid(2),
                reader.GetString(3).TrimEnd(),
                reader.GetDateTime(4),
                reader.GetDateTime(5),
                reader.GetDateTime(6),
                NullableDateTime(reader, 7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                (byte[])reader.GetValue(9)));
        }

        return tokens;
    }

    private DbCommand CreateCommand(UnitOfWorkScope scope, string sql)
    {
        var command = scope.Connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        return command;
    }

    // The token hash is never written to the audit log: it is a credential-derived value.
    private static Dictionary<string, object?> CreatedValues(RefreshToken token) =>
        new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["UserId"] = token.UserId,
            ["FamilyId"] = token.FamilyId,
            ["TokenHash"] = "[redacted]",
            ["IssuedDate"] = token.IssuedUtc,
            ["ExpiresDate"] = token.ExpiresUtc,
            ["LastUsedDate"] = token.LastUsedUtc,
        };

    private static DateTime? NullableDateTime(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
}
