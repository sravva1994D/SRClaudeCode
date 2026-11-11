using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Application.Patients.Handlers;

/// <summary>Records that a user opened an active patient. A deactivated or unknown patient is not recorded.</summary>
public sealed class RecordPatientViewedCommandHandler(
    IUnitOfWork unitOfWork,
    IPatientRepository patients,
    IRecentPatientRepository recentPatients,
    IClock clock)
{
    public Task HandleAsync(RecordPatientViewedCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            _ = await patients.FindActiveAsync(command.PatientId, token)
                ?? throw new RecordNotFoundException("Patient");

            await recentPatients.RecordViewAsync(
                command.UserId,
                command.PatientId,
                clock.UtcNow,
                RecentPatientsPolicy.KeepCount,
                token);
        }, cancellationToken);
}

/// <summary>Returns the patients a user opened most recently, newest first.</summary>
public sealed class ListRecentPatientsQueryHandler(IPatientSearch patientSearch)
{
    public Task<IReadOnlyList<PatientListItem>> HandleAsync(
        ListRecentPatientsQuery query,
        CancellationToken cancellationToken) =>
        patientSearch.ListRecentAsync(query.UserId, RecentPatientsPolicy.KeepCount, cancellationToken);
}
