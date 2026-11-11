using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
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

    private const string FindActiveSql = """
        SELECT AppointmentId, PatientId, SlotStart, AppointmentStatusId, IsActive, RowVersion
        FROM dbo.Appointments
        WHERE AppointmentId = @AppointmentId AND IsActive = 1;
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
