using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Visits;

namespace PatientManagement.Application.Visits.Handlers;

/// <summary>
/// Starts a visit from a scheduled appointment. The appointment is checked, the visit is inserted, and the audit row is
/// written, all in one unit of work. The appointment's own status is unchanged until the visit is completed.
/// </summary>
public sealed class StartVisitCommandHandler(
    IUnitOfWork unitOfWork,
    IAppointmentRepository appointments,
    IVisitRepository visits)
{
    public Task<VisitDetails> HandleAsync(StartVisitCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var appointment = await appointments.FindActiveAsync(command.AppointmentId, token)
                ?? throw new RecordNotFoundException("Appointment");
            appointment.EnsureCanStartVisit();

            var visit = Visit.Start(appointment.PatientId, appointment.AppointmentId);
            await visits.AddAsync(visit, command.ChangedByUserId, token);
            return VisitDetails.From(visit);
        }, cancellationToken);
}

/// <summary>Reads one active visit.</summary>
public sealed class GetVisitQueryHandler(IVisitRepository visits)
{
    public async Task<VisitDetails> HandleAsync(GetVisitQuery query, CancellationToken cancellationToken)
    {
        var visit = await visits.FindActiveAsync(query.VisitId, cancellationToken)
            ?? throw new RecordNotFoundException("Visit");
        return VisitDetails.From(visit);
    }
}
