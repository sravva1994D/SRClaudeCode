using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Clinic;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Reads and saves the single clinic profile row inside the current unit of work.</summary>
public sealed class SqlClinicProfileRepository(
    IAuditLogWriter auditLogWriter,
    IClock clock,
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IClinicProfileRepository
{
    private const string TableName = "ClinicProfile";
    private const string RecordId = "1";

    private const string GetSql = """
        SELECT ClinicName, DoctorName, Qualifications, RegistrationNumber, Address, PhoneNumber, FooterNotes, RowVersion
        FROM dbo.ClinicProfile
        WHERE ClinicProfileId = 1 AND IsActive = 1;
        """;

    private const string UpdateSql = """
        UPDATE dbo.ClinicProfile
        SET ClinicName = @ClinicName,
            DoctorName = @DoctorName,
            Qualifications = @Qualifications,
            RegistrationNumber = @RegistrationNumber,
            Address = @Address,
            PhoneNumber = @PhoneNumber,
            FooterNotes = @FooterNotes,
            ModifiedDate = @ModifiedDate,
            ModifiedBy = @ModifiedBy
        OUTPUT inserted.RowVersion
        WHERE ClinicProfileId = 1 AND RowVersion = @RowVersion;
        """;

    public async Task<ClinicProfile> GetAsync(CancellationToken cancellationToken)
    {
        // Reads outside a unit of work use their own connection and no transaction (CLAUDE.md 6.3).
        var scope = UnitOfWorkScope.TryGetCurrent();
        if (scope is not null)
        {
            return await ReadAsync(scope.Connection, scope.Transaction, cancellationToken);
        }

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await ReadAsync(connection, transaction: null, cancellationToken);
    }

    private async Task<ClinicProfile> ReadAsync(DbConnection connection, DbTransaction? transaction, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = GetSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = transaction;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("The clinic profile row is missing. Run the migrations.");
        }

        return ClinicProfile.Reconstitute(
            NullableString(reader, 0),
            NullableString(reader, 1),
            NullableString(reader, 2),
            NullableString(reader, 3),
            NullableString(reader, 4),
            NullableString(reader, 5),
            NullableString(reader, 6),
            (byte[])reader.GetValue(7));
    }

    public async Task SaveAsync(
        ClinicProfile profile,
        IReadOnlyDictionary<string, object?> auditValuesBefore,
        int changedByUserId,
        CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = CreateCommand(scope, UpdateSql);
        command.Parameters.Add(Nullable("@ClinicName", profile.ClinicName));
        command.Parameters.Add(Nullable("@DoctorName", profile.DoctorName));
        command.Parameters.Add(Nullable("@Qualifications", profile.Qualifications));
        command.Parameters.Add(Nullable("@RegistrationNumber", profile.RegistrationNumber));
        command.Parameters.Add(Nullable("@Address", profile.Address));
        command.Parameters.Add(Nullable("@PhoneNumber", profile.PhoneNumber));
        command.Parameters.Add(Nullable("@FooterNotes", profile.FooterNotes));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));
        command.Parameters.Add(new SqlParameter("@ModifiedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@RowVersion", profile.RowVersion));

        var newRowVersion = await command.ExecuteScalarAsync(cancellationToken) as byte[]
            ?? throw new ConcurrencyConflictException(TableName);
        profile.SetRowVersion(newRowVersion);

        await auditLogWriter.WriteAsync(
            new AuditEntry(TableName, RecordId, AuditAction.Update, auditValuesBefore, profile.AuditValues(), changedByUserId),
            cancellationToken);
    }

    private DbCommand CreateCommand(UnitOfWorkScope scope, string sql)
    {
        var command = scope.Connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        return command;
    }

    private static SqlParameter Nullable(string name, string? value) => new(name, (object?)value ?? DBNull.Value);

    private static string? NullableString(DbDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
}
