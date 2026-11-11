using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Reads and saves patients inside the current unit of work. Every write is audited in the same transaction.</summary>
public sealed class SqlPatientRepository(
    IAuditLogWriter auditLogWriter,
    IClock clock,
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IPatientRepository
{
    private const string TableName = "Patients";

    private const string FindActiveSql = """
        SELECT PatientId, FullName, DateOfBirth, GenderId, PhoneNumber, Address, IsActive, RowVersion
        FROM dbo.Patients
        WHERE PatientId = @PatientId AND IsActive = 1;
        """;

    // Name equality relies on the database's case-insensitive collation, which the development database uses.
    // The name is trimmed on write, so comparing without trimming here is correct.
    private const string FindDuplicatesSql = """
        SELECT TOP (@MaximumCount) PatientId, FullName, DateOfBirth, GenderId, PhoneNumber, Address, IsActive, RowVersion
        FROM dbo.Patients
        WHERE IsActive = 1
          AND DateOfBirth = @DateOfBirth
          AND FullName = @FullName
          AND (@ExcludePatientId IS NULL OR PatientId <> @ExcludePatientId)
        ORDER BY PatientId;
        """;

    // OUTPUT returns the new id and row version, so no second round trip is needed.
    private const string InsertSql = """
        INSERT INTO dbo.Patients (FullName, DateOfBirth, GenderId, PhoneNumber, Address, CreatedBy, ModifiedDate, ModifiedBy)
        OUTPUT inserted.PatientId, inserted.RowVersion
        VALUES (@FullName, @DateOfBirth, @GenderId, @PhoneNumber, @Address, @ChangedBy, @ModifiedDate, @ChangedBy);
        """;

    // A deactivated patient is updated with IsActive = 0, so the write path does not need its own statement.
    // Zero rows means the row changed since it was read, which is a concurrency conflict (CLAUDE.md 6.3).
    private const string UpdateSql = """
        UPDATE dbo.Patients
        SET FullName = @FullName,
            DateOfBirth = @DateOfBirth,
            GenderId = @GenderId,
            PhoneNumber = @PhoneNumber,
            Address = @Address,
            IsActive = @IsActive,
            ModifiedDate = @ModifiedDate,
            ModifiedBy = @ChangedBy
        OUTPUT inserted.RowVersion
        WHERE PatientId = @PatientId AND RowVersion = @RowVersion;
        """;

    public async Task<Patient?> FindActiveAsync(int patientId, CancellationToken cancellationToken)
    {
        // Reads outside a unit of work use their own connection and no transaction (CLAUDE.md 6.3).
        var scope = UnitOfWorkScope.TryGetCurrent();
        if (scope is not null)
        {
            return await ReadAsync(scope.Connection, scope.Transaction, patientId, cancellationToken);
        }

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await ReadAsync(connection, transaction: null, patientId, cancellationToken);
    }

    public async Task<IReadOnlyList<Patient>> FindActiveDuplicatesAsync(
        string fullName,
        DateOnly dateOfBirth,
        int? excludePatientId,
        int maximumCount,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = FindDuplicatesSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@MaximumCount", maximumCount));
        command.Parameters.Add(new SqlParameter("@FullName", fullName));
        command.Parameters.Add(new SqlParameter("@DateOfBirth", dateOfBirth.ToDateTime(TimeOnly.MinValue)));
        command.Parameters.Add(new SqlParameter("@ExcludePatientId", SqlDbType.Int)
        {
            Value = (object?)excludePatientId ?? DBNull.Value,
        });

        var matches = new List<Patient>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            matches.Add(ReadPatient(reader));
        }

        return matches;
    }

    public async Task AddAsync(Patient patient, int changedByUserId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = CreateCommand(scope, InsertSql);
        AddFieldParameters(command, patient);
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("The patient insert returned no row.");
        }

        patient.AssignPatientId(reader.GetInt32(0));
        patient.SetRowVersion((byte[])reader.GetValue(1));
        await reader.CloseAsync();

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                RecordIdOf(patient),
                AuditAction.Create,
                OldValues: null,
                NewValues: patient.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    public async Task SaveAsync(
        Patient patient,
        IReadOnlyDictionary<string, object?> auditValuesBefore,
        AuditAction action,
        int changedByUserId,
        CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = CreateCommand(scope, UpdateSql);
        AddFieldParameters(command, patient);
        command.Parameters.Add(new SqlParameter("@IsActive", patient.IsActive));
        command.Parameters.Add(new SqlParameter("@PatientId", patient.PatientId));
        command.Parameters.Add(new SqlParameter("@RowVersion", patient.RowVersion));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));

        var newRowVersion = await command.ExecuteScalarAsync(cancellationToken) as byte[]
            ?? throw new ConcurrencyConflictException(TableName);
        patient.SetRowVersion(newRowVersion);

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                RecordIdOf(patient),
                action,
                auditValuesBefore,
                patient.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    private static string RecordIdOf(Patient patient) =>
        patient.PatientId.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private async Task<Patient?> ReadAsync(
        DbConnection connection,
        DbTransaction? transaction,
        int patientId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = FindActiveSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = transaction;
        command.Parameters.Add(new SqlParameter("@PatientId", patientId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadPatient(reader);
    }

    private static Patient ReadPatient(DbDataReader reader) => Patient.Reconstitute(
        reader.GetInt32(0),
        reader.GetString(1),
        DateOnly.FromDateTime(reader.GetDateTime(2)),
        reader.GetInt32(3),
        NullableString(reader, 4),
        NullableString(reader, 5),
        reader.GetBoolean(6),
        (byte[])reader.GetValue(7));

    private static void AddFieldParameters(DbCommand command, Patient patient)
    {
        command.Parameters.Add(new SqlParameter("@FullName", patient.FullName));
        command.Parameters.Add(new SqlParameter("@DateOfBirth", patient.DateOfBirth.ToDateTime(TimeOnly.MinValue)));
        command.Parameters.Add(new SqlParameter("@GenderId", patient.GenderId));
        command.Parameters.Add(Nullable("@PhoneNumber", patient.PhoneNumber));
        command.Parameters.Add(Nullable("@Address", patient.Address));
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
