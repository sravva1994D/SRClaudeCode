using PatientManagement.Application.Paging;
using PatientManagement.Application.Patients;

namespace PatientManagement.Application.Interfaces;

/// <summary>Reads one page of active patients for the list and search. Read-only, so it runs outside a unit of work.</summary>
public interface IPatientSearch
{
    /// <summary>
    /// Filters by name (contains, ignoring case) and by phone digits (contains). A null filter is not applied. The
    /// result is ordered by name, then id, so paging is stable.
    /// </summary>
    Task<PagedResponse<PatientListItem>> SearchActiveAsync(
        string? nameTerm,
        string? phoneDigits,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the user's most recently opened active patients, newest first. A patient deactivated after being opened
    /// is left out.
    /// </summary>
    Task<IReadOnlyList<PatientListItem>> ListRecentAsync(
        int userId,
        int limit,
        CancellationToken cancellationToken);
}
