using PatientManagement.Domain.Consultation;

namespace PatientManagement.Application.Interfaces;

/// <summary>Reads and saves a visit's medications. Saves run inside a unit of work and are audited in the same transaction.</summary>
public interface IMedicationRepository
{
    /// <summary>Returns the visit's active medications in entry order.</summary>
    Task<IReadOnlyList<Medication>> ListActiveForVisitAsync(int visitId, CancellationToken cancellationToken);

    /// <summary>Inserts a medication, sets its id and row version, and writes the audit row.</summary>
    Task AddAsync(Medication medication, int changedByUserId, CancellationToken cancellationToken);

    /// <summary>Deactivates a medication that a newer draft replaces, and writes the audit row.</summary>
    Task DeactivateAsync(Medication medication, int changedByUserId, CancellationToken cancellationToken);
}

/// <summary>
/// Finds medicine names from the doctor's own history for the autocomplete. Read-only. Only active medications count, and
/// each name is returned once.
/// </summary>
public interface IMedicineSuggestionRepository
{
    /// <summary>Returns up to <paramref name="limit"/> distinct names that start with the prefix, in alphabetical order.</summary>
    Task<IReadOnlyList<string>> FindNamesStartingWithAsync(string prefix, int limit, CancellationToken cancellationToken);
}
