using PatientManagement.Application.Paging;
using PatientManagement.Application.Visits;

namespace PatientManagement.Application.Interfaces;

/// <summary>Reads a patient's visit history. Read-only: active visits only.</summary>
public interface IVisitHistoryRepository
{
    /// <summary>Returns one page of the patient's active visits in the query's date range, newest first.</summary>
    Task<PagedResponse<PatientVisitHistoryItem>> ListForPatientAsync(
        PatientVisitHistoryQuery query,
        CancellationToken cancellationToken);
}
