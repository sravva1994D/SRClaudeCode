using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Paging;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Application.Visits.Handlers;

/// <summary>Returns one page of a patient's visit history. The patient must exist and be active.</summary>
public sealed class PatientVisitHistoryHandler(
    IPatientRepository patients,
    IVisitHistoryRepository history)
{
    public async Task<PagedResponse<PatientVisitHistoryItem>> HandleAsync(
        PatientVisitHistoryQuery query,
        CancellationToken cancellationToken)
    {
        _ = await patients.FindActiveAsync(query.PatientId, cancellationToken)
            ?? throw new RecordNotFoundException("Patient");

        return await history.ListForPatientAsync(query, cancellationToken);
    }
}
