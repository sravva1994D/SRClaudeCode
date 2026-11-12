using PatientManagement.Domain.Prescriptions;

namespace PatientManagement.Application.Interfaces;

/// <summary>Reads and saves prescriptions. Saves run inside a unit of work and are audited in the same transaction.</summary>
public interface IPrescriptionRepository
{
    /// <summary>Returns the active prescription for the visit, or null when none has been issued.</summary>
    Task<Prescription?> FindActiveForVisitAsync(int visitId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the prescription and writes its audit row. The filtered unique index allows one active prescription per visit;
    /// a second insert throws <see cref="Domain.Exceptions.ConcurrencyConflictException"/>.
    /// </summary>
    Task AddAsync(Prescription prescription, int changedByUserId, CancellationToken cancellationToken);
}
