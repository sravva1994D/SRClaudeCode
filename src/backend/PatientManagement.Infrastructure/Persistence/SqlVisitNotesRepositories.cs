using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Consultation;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Reads and saves a visit's complaints inside the current unit of work. Every change is audited in the same transaction.</summary>
public sealed class SqlComplaintRepository(
    IAuditLogWriter auditLogWriter,
    IClock clock,
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IComplaintRepository
{
    private const string TableName = "Complaints";

    private const string ListActiveSql = """
        SELECT ComplaintId, VisitId, ComplaintText, SortOrder, IsActive, RowVersion
        FROM dbo.Complaints
        WHERE VisitId = @VisitId AND IsActive = 1
        ORDER BY SortOrder, ComplaintId;
        """;

    private const string InsertSql = """
        INSERT INTO dbo.Complaints (VisitId, ComplaintText, SortOrder, CreatedBy, ModifiedDate, ModifiedBy)
        OUTPUT inserted.ComplaintId, inserted.RowVersion
        VALUES (@VisitId, @Text, @SortOrder, @ChangedBy, @ModifiedDate, @ChangedBy);
        """;

    // Only an active row can be deactivated. Zero rows affected means the row changed under the caller.
    private const string DeactivateSql = """
        UPDATE dbo.Complaints
        SET IsActive = 0, ModifiedDate = @ModifiedDate, ModifiedBy = @ChangedBy
        WHERE ComplaintId = @ComplaintId AND IsActive = 1;
        """;

    public async Task<IReadOnlyList<Complaint>> ListActiveForVisitAsync(int visitId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.TryGetCurrent();
        if (scope is not null)
        {
            return await ReadAsync(scope.Connection, scope.Transaction, visitId, cancellationToken);
        }

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await ReadAsync(connection, transaction: null, visitId, cancellationToken);
    }

    public async Task AddAsync(Complaint complaint, int changedByUserId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = scope.Connection.CreateCommand();
        command.CommandText = InsertSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@VisitId", complaint.VisitId));
        command.Parameters.Add(new SqlParameter("@Text", complaint.Text));
        command.Parameters.Add(new SqlParameter("@SortOrder", complaint.SortOrder));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("The complaint insert returned no row.");
        }

        complaint.AssignComplaintId(reader.GetInt32(0));
        complaint.SetRowVersion((byte[])reader.GetValue(1));
        await reader.CloseAsync();

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                complaint.ComplaintId.ToString(CultureInfo.InvariantCulture),
                AuditAction.Create,
                OldValues: null,
                NewValues: complaint.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    public async Task DeactivateAsync(Complaint complaint, int changedByUserId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        var before = complaint.AuditValues();
        complaint.Deactivate();

        await using var command = scope.Connection.CreateCommand();
        command.CommandText = DeactivateSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@ComplaintId", complaint.ComplaintId));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));

        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new InvalidOperationException("The complaint to deactivate was not active.");
        }

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                complaint.ComplaintId.ToString(CultureInfo.InvariantCulture),
                AuditAction.Update,
                OldValues: before,
                NewValues: complaint.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    private static async Task<IReadOnlyList<Complaint>> ReadAsync(
        DbConnection connection,
        DbTransaction? transaction,
        int visitId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = ListActiveSql;
        command.Transaction = transaction;
        command.Parameters.Add(new SqlParameter("@VisitId", visitId));

        var complaints = new List<Complaint>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            complaints.Add(Complaint.Reconstitute(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetBoolean(4),
                (byte[])reader.GetValue(5)));
        }

        return complaints;
    }
}

/// <summary>Reads and saves a visit's diagnoses inside the current unit of work. Every change is audited in the same transaction.</summary>
public sealed class SqlDiagnosisRepository(
    IAuditLogWriter auditLogWriter,
    IClock clock,
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IDiagnosisRepository
{
    private const string TableName = "Diagnoses";

    private const string ListActiveSql = """
        SELECT DiagnosisId, VisitId, DiagnosisText, DiagnosisCode, SortOrder, IsActive, RowVersion
        FROM dbo.Diagnoses
        WHERE VisitId = @VisitId AND IsActive = 1
        ORDER BY SortOrder, DiagnosisId;
        """;

    private const string InsertSql = """
        INSERT INTO dbo.Diagnoses (VisitId, DiagnosisText, DiagnosisCode, SortOrder, CreatedBy, ModifiedDate, ModifiedBy)
        OUTPUT inserted.DiagnosisId, inserted.RowVersion
        VALUES (@VisitId, @Text, @Code, @SortOrder, @ChangedBy, @ModifiedDate, @ChangedBy);
        """;

    private const string DeactivateSql = """
        UPDATE dbo.Diagnoses
        SET IsActive = 0, ModifiedDate = @ModifiedDate, ModifiedBy = @ChangedBy
        WHERE DiagnosisId = @DiagnosisId AND IsActive = 1;
        """;

    public async Task<IReadOnlyList<Diagnosis>> ListActiveForVisitAsync(int visitId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.TryGetCurrent();
        if (scope is not null)
        {
            return await ReadAsync(scope.Connection, scope.Transaction, visitId, cancellationToken);
        }

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await ReadAsync(connection, transaction: null, visitId, cancellationToken);
    }

    public async Task AddAsync(Diagnosis diagnosis, int changedByUserId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = scope.Connection.CreateCommand();
        command.CommandText = InsertSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@VisitId", diagnosis.VisitId));
        command.Parameters.Add(new SqlParameter("@Text", diagnosis.Text));
        command.Parameters.Add(new SqlParameter("@Code", (object?)diagnosis.Code ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@SortOrder", diagnosis.SortOrder));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("The diagnosis insert returned no row.");
        }

        diagnosis.AssignDiagnosisId(reader.GetInt32(0));
        diagnosis.SetRowVersion((byte[])reader.GetValue(1));
        await reader.CloseAsync();

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                diagnosis.DiagnosisId.ToString(CultureInfo.InvariantCulture),
                AuditAction.Create,
                OldValues: null,
                NewValues: diagnosis.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    public async Task DeactivateAsync(Diagnosis diagnosis, int changedByUserId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        var before = diagnosis.AuditValues();
        diagnosis.Deactivate();

        await using var command = scope.Connection.CreateCommand();
        command.CommandText = DeactivateSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@DiagnosisId", diagnosis.DiagnosisId));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));

        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new InvalidOperationException("The diagnosis to deactivate was not active.");
        }

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                diagnosis.DiagnosisId.ToString(CultureInfo.InvariantCulture),
                AuditAction.Update,
                OldValues: before,
                NewValues: diagnosis.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    private static async Task<IReadOnlyList<Diagnosis>> ReadAsync(
        DbConnection connection,
        DbTransaction? transaction,
        int visitId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = ListActiveSql;
        command.Transaction = transaction;
        command.Parameters.Add(new SqlParameter("@VisitId", visitId));

        var diagnoses = new List<Diagnosis>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            diagnoses.Add(Diagnosis.Reconstitute(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetInt32(4),
                reader.GetBoolean(5),
                (byte[])reader.GetValue(6)));
        }

        return diagnoses;
    }
}
