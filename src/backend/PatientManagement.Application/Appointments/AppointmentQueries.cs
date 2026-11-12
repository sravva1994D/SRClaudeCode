namespace PatientManagement.Application.Appointments;

/// <summary>Lists the active appointments that start in [FromUtc, ToUtc). Both bounds are UTC.</summary>
public sealed record ListAppointmentsQuery(DateTime FromUtc, DateTime ToUtc);

/// <summary>
/// One row of the calendar: the slot, the patient's name, the status, and the active visit started from it (null when
/// no visit has started). The dashboard uses the visit ID to open an existing visit instead of starting a second one.
/// </summary>
public sealed record AppointmentListItem(
    int AppointmentId,
    int PatientId,
    string PatientFullName,
    DateTime SlotStartUtc,
    int StatusId,
    int? VisitId);
