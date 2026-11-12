using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Prescriptions;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Reads and saves prescriptions inside the current unit of work. Every insert is audited in the same transaction.</summary>
public sealed class SqlPrescriptionRepository(
    IAuditLogWriter auditLogWriter,
    IClock clock,
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IPrescriptionRepository
{
    private const string TableName = "Prescriptions";

    // SQL Server error numbers for a unique index or unique constraint violation.
    private const int DuplicateKeyErrorNumber = 2601;
    private const int UniqueConstraintErrorNumber = 2627;

    private const string FindActiveForVisitSql = """
        SELECT PrescriptionId, VisitId, PrescriptionDate, IsActive, RowVersion
        FROM dbo.Prescriptions
        WHERE VisitId = @VisitId AND IsActive = 1;
        """;

    // The filtered unique index UX_Prescriptions_VisitId rejects a second active prescription for the same visit.
    private const string InsertSql = """
        INSERT INTO dbo.Prescriptions (VisitId, PrescriptionDate, CreatedBy, ModifiedDate, ModifiedBy)
        OUTPUT inserted.PrescriptionId, inserted.RowVersion
        VALUES (@VisitId, @PrescriptionDate, @ChangedBy, @ModifiedDate, @ChangedBy);
        """;

    public async Task<Prescription?> FindActiveForVisitAsync(int visitId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.TryGetCurrent();
        if (scope is not null)
        {
            return await ReadAsync(scope.Connection, scope.Transaction, visitId, cancellationToken);
        }

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await ReadAsync(connection, transaction: null, visitId, cancellationToken);
    }

    public async Task AddAsync(Prescription prescription, int changedByUserId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = scope.Connection.CreateCommand();
        command.CommandText = InsertSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@VisitId", prescription.VisitId));
        command.Parameters.Add(new SqlParameter("@PrescriptionDate", prescription.PrescriptionDate.ToDateTime(TimeOnly.MinValue)));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("The prescription insert returned no row.");
            }

            prescription.AssignPrescriptionId(reader.GetInt32(0));
            prescription.SetRowVersion((byte[])reader.GetValue(1));
        }
        catch (SqlException exception) when (exception.Number is DuplicateKeyErrorNumber or UniqueConstraintErrorNumber)
        {
            // A prescription for this visit was issued by another request; the caller can read it back.
            throw new ConcurrencyConflictException(TableName);
        }

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                prescription.PrescriptionId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                AuditAction.Create,
                OldValues: null,
                NewValues: prescription.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    private static async Task<Prescription?> ReadAsync(
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

        return Prescription.Reconstitute(
            reader.GetInt32(0),
            reader.GetInt32(1),
            DateOnly.FromDateTime(reader.GetDateTime(2)),
            reader.GetBoolean(3),
            (byte[])reader.GetValue(4));
    }
}
