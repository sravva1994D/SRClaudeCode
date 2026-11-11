namespace PatientManagement.Application.Appointments;

/// <summary>Lists the active appointments that start in [FromUtc, ToUtc). Both bounds are UTC.</summary>
public sealed record ListAppointmentsQuery(DateTime FromUtc, DateTime ToUtc);

/// <summary>One row of the calendar: the slot, the patient's name, and the status.</summary>
public sealed record AppointmentListItem(
    int AppointmentId,
    int PatientId,
    string PatientFullName,
    DateTime SlotStartUtc,
    int StatusId);
