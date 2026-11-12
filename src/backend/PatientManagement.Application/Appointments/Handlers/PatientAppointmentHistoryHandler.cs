using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Paging;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Application.Appointments.Handlers;

/// <summary>Returns one page of a patient's appointments. The patient must exist and be active.</summary>
public sealed class PatientAppointmentHistoryHandler(
    IPatientRepository patients,
    IAppointmentRepository appointments)
{
    public async Task<PagedResponse<PatientAppointmentHistoryItem>> HandleAsync(
        PatientAppointmentHistoryQuery query,
        CancellationToken cancellationToken)
    {
        _ = await patients.FindActiveAsync(query.PatientId, cancellationToken)
            ?? throw new RecordNotFoundException("Patient");

        return await appointments.ListForPatientAsync(
            query.PatientId,
            query.PageNumber,
            query.PageSize,
            cancellationToken);
    }
}
