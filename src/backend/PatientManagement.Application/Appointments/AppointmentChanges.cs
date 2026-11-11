using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Application.Appointments;

/// <summary>Moves a scheduled appointment to a new future slot.</summary>
public sealed record RescheduleAppointmentCommand(int AppointmentId, DateTime SlotStartUtc, int ChangedByUserId);

/// <summary>Sets the outcome of a scheduled appointment: completed, cancelled or no-show.</summary>
public sealed record ChangeAppointmentStatusCommand(int AppointmentId, int TargetStatusId, int ChangedByUserId);

/// <summary>
/// Both changes load the appointment, apply the domain rule, save, and write the audit row inside one unit of work, so a
/// failure at any step leaves nothing changed.
/// </summary>
public sealed class RescheduleAppointmentCommandHandler(
    IUnitOfWork unitOfWork,
    IAppointmentRepository appointments,
    IClock clock)
{
    public Task<AppointmentDetails> HandleAsync(RescheduleAppointmentCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var appointment = await appointments.FindActiveAsync(command.AppointmentId, token)
                ?? throw new RecordNotFoundException("Appointment");
            var auditValuesBefore = appointment.AuditValues();

            appointment.Reschedule(command.SlotStartUtc, clock.UtcNow);

            await appointments.SaveAsync(appointment, auditValuesBefore, AuditAction.Update, command.ChangedByUserId, token);
            return AppointmentDetails.From(appointment);
        }, cancellationToken);
}

public sealed class ChangeAppointmentStatusCommandHandler(
    IUnitOfWork unitOfWork,
    IAppointmentRepository appointments)
{
    public Task<AppointmentDetails> HandleAsync(ChangeAppointmentStatusCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var appointment = await appointments.FindActiveAsync(command.AppointmentId, token)
                ?? throw new RecordNotFoundException("Appointment");
            var auditValuesBefore = appointment.AuditValues();

            appointment.ChangeStatus(command.TargetStatusId);

            await appointments.SaveAsync(appointment, auditValuesBefore, AuditAction.Update, command.ChangedByUserId, token);
            return AppointmentDetails.From(appointment);
        }, cancellationToken);
}
