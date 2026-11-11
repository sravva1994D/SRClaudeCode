using PatientManagement.Domain.Clinic;

namespace PatientManagement.Application.Interfaces;

/// <summary>Reads and saves the clinic profile. Saves must run inside a unit of work and are audited.</summary>
public interface IClinicProfileRepository
{
    /// <summary>Returns the single clinic profile row.</summary>
    Task<ClinicProfile> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(
        ClinicProfile profile,
        IReadOnlyDictionary<string, object?> auditValuesBefore,
        int changedByUserId,
        CancellationToken cancellationToken);
}
