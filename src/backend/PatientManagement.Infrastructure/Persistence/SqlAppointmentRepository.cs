using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Appointments;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Paging;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Reads and saves appointments inside the current unit of work. Every booking is audited in the same transaction.</summary>
public sealed class SqlAppointmentRepository(
    IAuditLogWriter auditLogWriter,
    IClock clock,
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IAppointmentRepository
{
    private const string TableName = "Appointments";

    // SQL Server error numbers for a unique index or unique constraint violation.
    private const int DuplicateKeyErrorNumber = 2601;
    private const int UniqueConstraintErrorNumber = 2627;

    // A row is saved only if it has not changed since it was read (RowVersion), so two users cannot overwrite each other.
    // Moving to a slot held by another active appointment is rejected by the same filtered unique index as booking.
    private const string UpdateSql = """
        UPDATE dbo.Appointments
        SET SlotStart = @SlotStart,
            AppointmentStatusId = @StatusId,
            ModifiedDate = @ModifiedDate,
            ModifiedBy = @ChangedBy
        OUTPUT inserted.RowVersion
        WHERE AppointmentId = @AppointmentId AND RowVersion = @RowVersion AND IsActive = 1;
        """;

    private const string FindActiveSql = """
        SELECT AppointmentId, PatientId, SlotStart, AppointmentStatusId, IsActive, RowVersion
        FROM dbo.Appointments
        WHERE AppointmentId = @AppointmentId AND IsActive = 1;
        """;

    // The calendar's range read. Uses the slot index for the range and joins the name only for the rows returned.
    // The visit join is LEFT because most appointments have no visit; UX_Visits_AppointmentId allows at most one
    // active visit per appointment, so the join never repeats a row.
    private const string ListBetweenSql = """
        SELECT a.AppointmentId, a.PatientId, p.FullName, a.SlotStart, a.AppointmentStatusId, v.VisitId
        FROM dbo.Appointments AS a
        INNER JOIN dbo.Patients AS p ON p.PatientId = a.PatientId
        LEFT JOIN dbo.Visits AS v ON v.AppointmentId = a.AppointmentId AND v.IsActive = 1
        WHERE a.IsActive = 1
          AND p.IsActive = 1
          AND a.SlotStart >= @FromUtc
          AND a.SlotStart < @ToUtc
        ORDER BY a.SlotStart, a.AppointmentId;
        """;

    // The count and the page share one filter, so the total always matches the rows that can be paged through.
    // The index IX_Appointments_PatientId_SlotStart serves both.
    private const string PatientHistorySql = """
        SELECT COUNT(*) FROM dbo.Appointments
        WHERE PatientId = @PatientId AND IsActive = 1;

        SELECT AppointmentId, SlotStart, AppointmentStatusId
        FROM dbo.Appointments
        WHERE PatientId = @PatientId AND IsActive = 1
        ORDER BY SlotStart DESC, AppointmentId DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    // The filtered unique index UX_Appointments_SlotStart rejects a second active booking for the same slot.
    private const string InsertSql = """
        INSERT INTO dbo.Appointments (PatientId, SlotStart, AppointmentStatusId, CreatedBy, ModifiedDate, ModifiedBy)
        OUTPUT inserted.AppointmentId, inserted.RowVersion
        VALUES (@PatientId, @SlotStart, @StatusId, @ChangedBy, @ModifiedDate, @ChangedBy);
        """;

    public async Task<Appointment?> FindActiveAsync(int appointmentId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.TryGetCurrent();
        if (scope is not null)
        {
            return await ReadAsync(scope.Connection, scope.Transaction, appointmentId, cancellationToken);
        }

        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await ReadAsync(connection, transaction: null, appointmentId, cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentListItem>> ListActiveBetweenAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = ListBetweenSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@FromUtc", DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc)));
        command.Parameters.Add(new SqlParameter("@ToUtc", DateTime.SpecifyKind(toUtc, DateTimeKind.Utc)));

        var items = new List<AppointmentListItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new AppointmentListItem(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetString(2),
                DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc),
                reader.GetInt32(4),
                reader.IsDBNull(5) ? null : reader.GetInt32(5)));
        }

        return items;
    }

    public async Task SaveAsync(
        Appointment appointment,
        IReadOnlyDictionary<string, object?> auditValuesBefore,
        AuditAction action,
        int changedByUserId,
        CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = scope.Connection.CreateCommand();
        command.CommandText = UpdateSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@SlotStart", DateTime.SpecifyKind(appointment.SlotStartUtc, DateTimeKind.Utc)));
        command.Parameters.Add(new SqlParameter("@StatusId", appointment.StatusId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@AppointmentId", appointment.AppointmentId));
        command.Parameters.Add(new SqlParameter("@RowVersion", appointment.RowVersion));

        byte[]? newRowVersion;
        try
        {
            newRowVersion = await command.ExecuteScalarAsync(cancellationToken) as byte[];
        }
        catch (SqlException exception) when (exception.Number is DuplicateKeyErrorNumber or UniqueConstraintErrorNumber)
        {
            throw new SlotAlreadyBookedException();
        }

        appointment.SetRowVersion(newRowVersion ?? throw new ConcurrencyConflictException(TableName));

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                appointment.AppointmentId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                action,
                auditValuesBefore,
                appointment.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    public async Task<PagedResponse<PatientAppointmentHistoryItem>> ListForPatientAsync(
        int patientId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = PatientHistorySql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@PatientId", patientId));
        command.Parameters.Add(new SqlParameter("@Offset", (pageNumber - 1) * pageSize));
        command.Parameters.Add(new SqlParameter("@PageSize", pageSize));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        var totalCount = reader.GetInt32(0);

        await reader.NextResultAsync(cancellationToken);
        var items = new List<PatientAppointmentHistoryItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PatientAppointmentHistoryItem(
                reader.GetInt32(0),
                DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc),
                reader.GetInt32(2)));
        }

        return new PagedResponse<PatientAppointmentHistoryItem>(items, totalCount, pageNumber, pageSize);
    }

    public async Task AddAsync(Appointment appointment, int changedByUserId, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = scope.Connection.CreateCommand();
        command.CommandText = InsertSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@PatientId", appointment.PatientId));
        command.Parameters.Add(new SqlParameter("@SlotStart", DateTime.SpecifyKind(appointment.SlotStartUtc, DateTimeKind.Utc)));
        command.Parameters.Add(new SqlParameter("@StatusId", appointment.StatusId));
        command.Parameters.Add(new SqlParameter("@ChangedBy", changedByUserId));
        command.Parameters.Add(new SqlParameter("@ModifiedDate", clock.UtcNow));

        try
        {
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("The appointment insert returned no row.");
            }

            appointment.AssignAppointmentId(reader.GetInt32(0));
            appointment.SetRowVersion((byte[])reader.GetValue(1));
        }
        catch (SqlException exception) when (exception.Number is DuplicateKeyErrorNumber or UniqueConstraintErrorNumber)
        {
            throw new SlotAlreadyBookedException();
        }

        await auditLogWriter.WriteAsync(
            new AuditEntry(
                TableName,
                appointment.AppointmentId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                AuditAction.Create,
                OldValues: null,
                NewValues: appointment.AuditValues(),
                changedByUserId),
            cancellationToken);
    }

    private async Task<Appointment?> ReadAsync(
        DbConnection connection,
        DbTransaction? transaction,
        int appointmentId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = FindActiveSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = transaction;
        command.Parameters.Add(new SqlParameter("@AppointmentId", appointmentId));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return Appointment.Reconstitute(
            reader.GetInt32(0),
            reader.GetInt32(1),
            DateTime.SpecifyKind(reader.GetDateTime(2), DateTimeKind.Utc),
            reader.GetInt32(3),
            reader.GetBoolean(4),
            (byte[])reader.GetValue(5));
    }
}
