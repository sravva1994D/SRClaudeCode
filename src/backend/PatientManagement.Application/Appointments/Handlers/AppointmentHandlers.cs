using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Application.Appointments.Handlers;

/// <summary>
/// Books a slot. The patient must exist and be active. The booking and its audit row commit together, and the database
/// rejects a second booking for the same slot.
/// </summary>
public sealed class ScheduleAppointmentCommandHandler(
    IUnitOfWork unitOfWork,
    IPatientRepository patients,
    IAppointmentRepository appointments,
    IClock clock)
{
    public Task<AppointmentDetails> HandleAsync(ScheduleAppointmentCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            _ = await patients.FindActiveAsync(command.PatientId, token)
                ?? throw new RecordNotFoundException("Patient");

            var appointment = Appointment.Schedule(command.PatientId, command.SlotStartUtc, clock.UtcNow);
            await appointments.AddAsync(appointment, command.ChangedByUserId, token);
            return AppointmentDetails.From(appointment);
        }, cancellationToken);
}

/// <summary>Reads one active appointment.</summary>
public sealed class GetAppointmentQueryHandler(IAppointmentRepository appointments)
{
    public async Task<AppointmentDetails> HandleAsync(GetAppointmentQuery query, CancellationToken cancellationToken)
    {
        var appointment = await appointments.FindActiveAsync(query.AppointmentId, cancellationToken)
            ?? throw new RecordNotFoundException("Appointment");
        return AppointmentDetails.From(appointment);
    }
}
