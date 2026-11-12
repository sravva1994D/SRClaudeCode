using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Visits;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Reads and saves visits inside the current unit of work. Every visit change is audited in the same transaction.</summary>
public sealed class SqlVisitRepository(
    IAuditLogWriter auditLogWriter,
    IClock clock,
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IVisitRepository
{
    private const string TableName = "Visits";

    // The row version check makes a status change fail when the visit changed since it was read (CLAUDE.md 6.3).
    private const string SaveStatusSql = """
        UPDATE dbo.Visits
        SET VisitStatusId = @StatusId, ModifiedDate = @ModifiedDate, ModifiedBy = @ChangedBy
        OUTPUT inserted.RowVersion
        WHERE VisitId = @VisitId AND IsActive = 1 AND RowVersion = @RowVersion;
        """;

    // SQL Server error numbers for a unique index or unique constraint violation.
    private const int DuplicateKeyErrorNumber = 2601;
    private const int UniqueConstraintErrorNumber = 2627;

    // Stamps the visit as changed by a note, medication or vitals edit. The row version check keeps the stamp from
    // overwriting a concurrent change that the caller has not seen.
    private const string MarkModifiedSql = """
        UPDATE dbo.Visits
        SET ModifiedDate = @ModifiedDate, ModifiedBy = @ChangedBy
        OUTPUT inserted.RowVersion
        WHERE VisitId = @VisitId AND IsActive = 1 AND RowVersion = @RowVersion;
        """;

    private const string FindActiveSql = """
        SELECT VisitId, PatientId, AppointmentId, VisitStatusId, IsActive, RowVersion, ModifiedDate
        FROM dbo.Visits
        WHERE VisitId = @VisitId AND IsActive = 1;
        """;

    // The update lock is held until the transaction ends, so a second writer for the same visit waits for the first.
    private const string FindActiveForUpdateSql = """
        SELECT VisitId, PatientId, AppointmentId, VisitStatusId, IsActive, RowVersion, ModifiedDate
        FROM dbo.Visits WITH (UPDLOCK, HOLDLOCK)
        WHERE VisitId = @VisitId AND IsActive = 1;
        """;

    // The filtered unique index UX_Visits_AppointmentId rejects a second active visit for the same appointment.
    private const string InsertSql = """
        INSERT INTO dbo.Visits (PatientId, AppointmentId, VisitStatusId, CreatedBy, ModifiedDate, ModifiedBy)
        OUTPUT inserted.VisitId, inserted.RowVersion
        VALUES (@PatientId, @AppointmentId, @StatusId, @ChangedBy, @ModifiedDate, @ChangedBy);
        """;

    public async Task<Visit?> FindActiveAsync(int visitId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.TryGetCurrent();
        if (scope is not null)
        {
            return await ReadAsync(scope.Connection, scope.Transaction, visitId, cancellationToken);
        }

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await ReadAsync(connection, transaction: null, visitId, cancellationToken);
    }

    public Task<Visit?> FindActiveForUpdateAsync(int visitId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        return ReadAsync(scope.Connection, scope.Transaction, visitId, cancellationToken, FindActiveForUpdateSql);
    }

    public async Task SaveStatusAsync(
        Visit visit,
        IReadOnlyDictionary<string, object?> auditValuesBefore,
        int changedByUserId,
        CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = scope.Connection.CreateCommand();
        command.CommandText = SaveStatusSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@VisitId", visit.VisitId));
        command.Parameters.Add(new SqlParameter("@StatusId", visit.StatusId));
        command.Parameters.Add(new SqlParameter("@RowVersion", visit.RowVersion));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));

        var newRowVersion = await command.ExecuteScalarAsync(cancellationToken) as byte[];
        visit.SetRowVersion(newRowVersion ?? throw new ConcurrencyConflictException(TableName));

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                visit.VisitId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                AuditAction.Update,
                OldValues: auditValuesBefore,
                NewValues: visit.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    public async Task MarkModifiedAsync(Visit visit, int changedByUserId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        var previousModifiedDate = visit.ModifiedDate;
        var modifiedDate = clock.UtcNow;

        await using var command = scope.Connection.CreateCommand();
        command.CommandText = MarkModifiedSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@VisitId", visit.VisitId));
        command.Parameters.Add(new SqlParameter("@RowVersion", visit.RowVersion));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", modifiedDate));

        var newRowVersion = await command.ExecuteScalarAsync(cancellationToken) as byte[];
        visit.SetRowVersion(newRowVersion ?? throw new ConcurrencyConflictException(TableName));
        visit.SetModifiedDate(modifiedDate);

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                visit.VisitId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                AuditAction.Update,
                OldValues: new Dictionary<string, object?> { ["ModifiedDate"] = previousModifiedDate },
                NewValues: new Dictionary<string, object?> { ["ModifiedDate"] = modifiedDate },
                changedByUserId),
            cancellationToken);
    }

    public async Task AddAsync(Visit visit, int changedByUserId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = scope.Connection.CreateCommand();
        command.CommandText = InsertSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@PatientId", visit.PatientId));
        command.Parameters.Add(new SqlParameter("@AppointmentId", (object?)visit.AppointmentId ?? DBNull.Value));
        command.Parameters.Add(new SqlParameter("@StatusId", visit.StatusId));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        var modifiedDate = clock.UtcNow;
        command.Parameters.Add(new SqlParameter("@ModifiedDate", modifiedDate));
        visit.SetModifiedDate(modifiedDate);

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("The visit insert returned no row.");
            }

            visit.AssignVisitId(reader.GetInt32(0));
            visit.SetRowVersion((byte[])reader.GetValue(1));
        }
        catch (SqlException exception) when (exception.Number is DuplicateKeyErrorNumber or UniqueConstraintErrorNumber)
        {
            throw new VisitAlreadyStartedException();
        }

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                visit.VisitId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                AuditAction.Create,
                OldValues: null,
                NewValues: visit.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    private static async Task<Visit?> ReadAsync(
        System.Data.Common.DbConnection connection,
        System.Data.Common.DbTransaction? transaction,
        int visitId,
        CancellationToken cancellationToken,
        string sql = FindActiveSql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.Parameters.Add(new SqlParameter("@VisitId", visitId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Visit.Reconstitute(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.IsDBNull(2) ? null : reader.GetInt32(2),
            reader.GetInt32(3),
            reader.GetBoolean(4),
            (byte[])reader.GetValue(5),
            // datetime2 comes back unspecified; the column holds UTC, so mark it as such before it reaches the API.
            DateTime.SpecifyKind(reader.GetDateTime(6), DateTimeKind.Utc));
    }
}
