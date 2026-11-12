using PatientManagement.Application.Auditing;
using PatientManagement.Domain.Visits;

namespace PatientManagement.Application.Interfaces;

/// <summary>Reads and saves visits. Saves run inside a unit of work and are audited in the same transaction.</summary>
public interface IVisitRepository
{
    /// <summary>Returns the active visit with this id, or null when there is none.</summary>
    Task<Visit?> FindActiveAsync(int visitId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a visit and sets its id and row version. Throws <see cref="Domain.Exceptions.VisitAlreadyStartedException"/>
    /// when the appointment already has an active visit.
    /// </summary>
    Task AddAsync(Visit visit, int changedByUserId, CancellationToken cancellationToken);
}
