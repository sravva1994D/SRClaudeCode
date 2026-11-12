using PatientManagement.Application.Auditing;
using PatientManagement.Domain.Visits;

namespace PatientManagement.Application.Interfaces;

/// <summary>Reads and saves visits. Saves run inside a unit of work and are audited in the same transaction.</summary>
public interface IVisitRepository
{
    /// <summary>Returns the active visit with this id, or null when there is none.</summary>
    Task<Visit?> FindActiveAsync(int visitId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the active visit and locks its row until the unit of work ends. Writes that depend on the visit's status
    /// call this first, so two saves for one visit run one after the other and see the current status.
    /// </summary>
    Task<Visit?> FindActiveForUpdateAsync(int visitId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a visit and sets its id and row version. Throws <see cref="Domain.Exceptions.VisitAlreadyStartedException"/>
    /// when the appointment already has an active visit.
    /// </summary>
    Task AddAsync(Visit visit, int changedByUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Saves the visit's status change and writes its audit row in the same transaction. The update checks the row version
    /// the visit was read with; a changed row throws <see cref="Domain.Exceptions.ConcurrencyConflictException"/>.
    /// </summary>
    Task SaveStatusAsync(
        Visit visit,
        IReadOnlyDictionary<string, object?> auditValuesBefore,
        int changedByUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stamps the visit as modified now (plan item 38) and writes an audit row for the new time. Called after any change to
    /// the visit's notes, medications or vitals. The update checks the row version the visit was read with.
    /// </summary>
    Task MarkModifiedAsync(Visit visit, int changedByUserId, CancellationToken cancellationToken);
}
