using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Consultation;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Reads the configured vital-sign ranges. A sign with no active row has no range, so nothing is checked for it.</summary>
public sealed class SqlVitalRangeRepository(
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IVitalRangeRepository
{
    private const string GetActiveSql = """
        SELECT VitalSign, NormalMinimum, NormalMaximum, PlausibleMinimum, PlausibleMaximum
        FROM dbo.VitalRanges
        WHERE IsActive = 1;
        """;

    public async Task<IReadOnlyDictionary<string, VitalRange>> GetActiveAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = GetActiveSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;

        var ranges = new Dictionary<string, VitalRange>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var range = new VitalRange(
                reader.GetString(0),
                reader.GetDecimal(1),
                reader.GetDecimal(2),
                reader.GetDecimal(3),
                reader.GetDecimal(4));
            ranges[range.VitalSign] = range;
        }

        return ranges;
    }
}

/// <summary>Reads and saves a visit's vitals inside the current unit of work. Every save is audited in the same transaction.</summary>
public sealed class SqlVitalsRepository(
    IAuditLogWriter auditLogWriter,
    IClock clock,
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IVitalsRepository
{
    private const string TableName = "Vitals";

    // SQL Server error numbers for a unique index or unique constraint violation.
    private const int DuplicateKeyErrorNumber = 2601;
    private const int UniqueConstraintErrorNumber = 2627;

    private const string FindActiveForVisitSql = """
        SELECT VitalsId, VisitId, Temperature, BloodPressureSystolic, BloodPressureDiastolic, Pulse, IsActive, RowVersion
        FROM dbo.Vitals
        WHERE VisitId = @VisitId AND IsActive = 1;
        """;

    // The filtered unique index UX_Vitals_VisitId rejects a second active vitals row for the same visit.
    private const string InsertSql = """
        INSERT INTO dbo.Vitals (VisitId, Temperature, BloodPressureSystolic, BloodPressureDiastolic, Pulse, CreatedBy, ModifiedDate, ModifiedBy)
        OUTPUT inserted.VitalsId, inserted.RowVersion
        VALUES (@VisitId, @Temperature, @Systolic, @Diastolic, @Pulse, @ChangedBy, @ModifiedDate, @ChangedBy);
        """;

    public async Task<Vitals?> FindActiveForVisitAsync(int visitId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.TryGetCurrent();
        if (scope is not null)
        {
            return await ReadAsync(scope.Connection, scope.Transaction, visitId, cancellationToken);
        }

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await ReadAsync(connection, transaction: null, visitId, cancellationToken);
    }

    public async Task AddAsync(Vitals vitals, int changedByUserId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = scope.Connection.CreateCommand();
        command.CommandText = InsertSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@VisitId", vitals.VisitId));
        command.Parameters.Add(new SqlParameter("@Temperature", (object?)vitals.Temperature ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@Systolic", (object?)vitals.BloodPressureSystolic ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@Diastolic", (object?)vitals.BloodPressureDiastolic ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@Pulse", (object?)vitals.Pulse ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("The vitals insert returned no row.");
            }

            vitals.AssignVitalsId(reader.GetInt32(0));
            vitals.SetRowVersion((byte[])reader.GetValue(1));
        }
        catch (SqlException exception) when (exception.Number is DuplicateKeyErrorNumber or UniqueConstraintErrorNumber)
        {
            throw new VitalsAlreadyRecordedException();
        }

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                vitals.VitalsId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                AuditAction.Create,
                OldValues: null,
                NewValues: vitals.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    private static async Task<Vitals?> ReadAsync(
        DbConnection connection,
        DbTransaction? transaction,
        int visitId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = FindActiveForVisitSql;
        command.Transaction = transaction;
        command.Parameters.Add(new SqlParameter("@VisitId", visitId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Vitals.Reconstitute(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.IsDBNull(2) ? null : reader.GetDecimal(2),
            reader.IsDBNull(3) ? null : reader.GetInt32(3),
            reader.IsDBNull(4) ? null : reader.GetInt32(4),
            reader.IsDBNull(5) ? null : reader.GetInt32(5),
            reader.GetBoolean(6),
            (byte[])reader.GetValue(7));
    }
}
