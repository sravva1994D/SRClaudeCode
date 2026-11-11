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
}
