using PatientManagement.Domain.Appointments;

namespace PatientManagement.Application.Appointments;

/// <summary>Books a 15-minute slot for a patient. <paramref name="SlotStartUtc"/> is the start of the slot, in UTC.</summary>
public sealed record ScheduleAppointmentCommand(int PatientId, DateTime SlotStartUtc, int ChangedByUserId);

public sealed record GetAppointmentQuery(int AppointmentId);

/// <summary>What the API returns for an appointment.</summary>
public sealed record AppointmentDetails(int AppointmentId, int PatientId, DateTime SlotStartUtc, int StatusId)
{
    public static AppointmentDetails From(Appointment appointment) => new(
        appointment.AppointmentId,
        appointment.PatientId,
        appointment.SlotStartUtc,
        appointment.StatusId);
}
