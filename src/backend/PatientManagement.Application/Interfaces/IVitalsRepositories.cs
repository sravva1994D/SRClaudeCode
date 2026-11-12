using PatientManagement.Application.Auditing;
using PatientManagement.Domain.Consultation;

namespace PatientManagement.Application.Interfaces;

/// <summary>Reads the configured vital-sign ranges (dbo.VitalRanges). Read-only.</summary>
public interface IVitalRangeRepository
{
    /// <summary>Returns the active ranges keyed by vital sign. A sign with no row has no configured range.</summary>
    Task<IReadOnlyDictionary<string, VitalRange>> GetActiveAsync(CancellationToken cancellationToken);
}

/// <summary>Reads and saves a visit's vitals. Saves run inside a unit of work and are audited in the same transaction.</summary>
public interface IVitalsRepository
{
    /// <summary>Returns the active vitals for the visit, or null when none are recorded.</summary>
    Task<Vitals?> FindActiveForVisitAsync(int visitId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the vitals and writes the audit row. Throws <see cref="Domain.Exceptions.VitalsAlreadyRecordedException"/>
    /// when the visit already has vitals.
    /// </summary>
    Task AddAsync(Vitals vitals, int changedByUserId, CancellationToken cancellationToken);
}
