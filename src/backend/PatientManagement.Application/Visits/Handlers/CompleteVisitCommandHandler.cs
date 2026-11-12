using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Application.Visits.Handlers;

/// <summary>
/// Completes a visit in one unit of work: the visit row is locked, the visit must be in progress and have vitals, the visit
/// becomes Completed, and its appointment becomes Completed. Each change writes its audit row in the same transaction, so a
/// failure at any step leaves the visit in progress and nothing else changed.
/// </summary>
public sealed class CompleteVisitCommandHandler(
    IUnitOfWork unitOfWork,
    IVisitRepository visits,
    IVitalsRepository vitals,
    IAppointmentRepository appointments)
{
    public Task<VisitDetails> HandleAsync(CompleteVisitCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var visit = await visits.FindActiveForUpdateAsync(command.VisitId, token)
                ?? throw new RecordNotFoundException("Visit");
            visit.EnsureInProgress();

            // The vitals are the visit's required record (BRD: vitals mandatory for any visit). The visit lock keeps them
            // from changing between this check and the status change.
            if (await vitals.FindActiveForVisitAsync(visit.VisitId, token) is null)
            {
                throw new DomainException("vitals_required", "Record the vitals before completing the visit.");
            }

            var visitBefore = visit.AuditValues();
            visit.Complete();
            await visits.SaveStatusAsync(visit, visitBefore, command.ChangedByUserId, token);

            if (visit.AppointmentId is int appointmentId)
            {
                var appointment = await appointments.FindActiveAsync(appointmentId, token)
                    ?? throw new RecordNotFoundException("Appointment");
                var appointmentBefore = appointment.AuditValues();
                appointment.ChangeStatus(AppointmentStatuses.Completed);
                await appointments.SaveAsync(appointment, appointmentBefore, AuditAction.Update, command.ChangedByUserId, token);
            }

            return VisitDetails.From(visit);
        }, cancellationToken);
}
