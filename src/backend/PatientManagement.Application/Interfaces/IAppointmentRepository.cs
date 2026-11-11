using PatientManagement.Application.Auditing;
using PatientManagement.Domain.Appointments;

namespace PatientManagement.Application.Interfaces;

/// <summary>Reads and saves appointments. Saves run inside a unit of work and are audited in the same transaction.</summary>
public interface IAppointmentRepository
{
    /// <summary>Returns the active appointment with this id, or null when there is none.</summary>
    Task<Appointment?> FindActiveAsync(int appointmentId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a new appointment and sets its id and row version. Throws <see cref="Domain.Exceptions.SlotAlreadyBookedException"/>
    /// when another active appointment already holds the slot.
    /// </summary>
    Task AddAsync(Appointment appointment, int changedByUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a change to an existing appointment and writes its audit row in the same transaction. Throws a concurrency
    /// conflict when the row changed since it was read, and <see cref="Domain.Exceptions.SlotAlreadyBookedException"/>
    /// when the new slot is held by another active appointment.
    /// </summary>
    Task SaveAsync(
        Appointment appointment,
        IReadOnlyDictionary<string, object?> auditValuesBefore,
        AuditAction action,
        int changedByUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the active appointments whose slot starts in [fromUtc, toUtc), ordered by slot start, with the patient's
    /// name. Read-only, so it runs outside a unit of work.
    /// </summary>
    Task<IReadOnlyList<Appointments.AppointmentListItem>> ListActiveBetweenAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}
