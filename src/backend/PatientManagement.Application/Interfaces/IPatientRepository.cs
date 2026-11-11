using PatientManagement.Application.Auditing;
using PatientManagement.Domain.Patients;

namespace PatientManagement.Application.Interfaces;

/// <summary>Reads and saves patients. Saves must run inside a unit of work and are audited in the same transaction.</summary>
public interface IPatientRepository
{
    /// <summary>Returns the active patient with this id, or null when there is none.</summary>
    Task<Patient?> FindActiveAsync(int patientId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns up to <paramref name="maximumCount"/> active patients with this name and date of birth. Names match
    /// without regard to case, and <paramref name="excludePatientId"/> leaves one patient out (the one being edited).
    /// </summary>
    Task<IReadOnlyList<Patient>> FindActiveDuplicatesAsync(
        string fullName,
        DateOnly dateOfBirth,
        int? excludePatientId,
        int maximumCount,
        CancellationToken cancellationToken);

    /// <summary>Inserts a new patient and sets its id and row version.</summary>
    Task AddAsync(Patient patient, int changedByUserId, CancellationToken cancellationToken);

    /// <summary>Saves changes to a patient. Throws a concurrency conflict when the row changed since it was read.</summary>
    Task SaveAsync(
        Patient patient,
        IReadOnlyDictionary<string, object?> auditValuesBefore,
        AuditAction action,
        int changedByUserId,
        CancellationToken cancellationToken);
}
