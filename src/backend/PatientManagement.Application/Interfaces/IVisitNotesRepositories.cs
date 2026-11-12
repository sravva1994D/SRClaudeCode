using PatientManagement.Domain.Consultation;

namespace PatientManagement.Application.Interfaces;

/// <summary>Reads and saves a visit's complaints. Saves run inside a unit of work and are audited in the same transaction.</summary>
public interface IComplaintRepository
{
    /// <summary>Returns the visit's active complaints in entry order.</summary>
    Task<IReadOnlyList<Complaint>> ListActiveForVisitAsync(int visitId, CancellationToken cancellationToken);

    /// <summary>Inserts a complaint, sets its id and row version, and writes the audit row.</summary>
    Task AddAsync(Complaint complaint, int changedByUserId, CancellationToken cancellationToken);

    /// <summary>Deactivates a complaint that a newer draft replaces, and writes the audit row.</summary>
    Task DeactivateAsync(Complaint complaint, int changedByUserId, CancellationToken cancellationToken);
}

/// <summary>Reads and saves a visit's diagnoses. Saves run inside a unit of work and are audited in the same transaction.</summary>
public interface IDiagnosisRepository
{
    /// <summary>Returns the visit's active diagnoses in entry order.</summary>
    Task<IReadOnlyList<Diagnosis>> ListActiveForVisitAsync(int visitId, CancellationToken cancellationToken);

    /// <summary>Inserts a diagnosis, sets its id and row version, and writes the audit row.</summary>
    Task AddAsync(Diagnosis diagnosis, int changedByUserId, CancellationToken cancellationToken);

    /// <summary>Deactivates a diagnosis that a newer draft replaces, and writes the audit row.</summary>
    Task DeactivateAsync(Diagnosis diagnosis, int changedByUserId, CancellationToken cancellationToken);
}
