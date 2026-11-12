namespace PatientManagement.Application.Appointments;

/// <summary>One page of a patient's appointments, newest first. Every status is included, so the history is complete.</summary>
public sealed record PatientAppointmentHistoryQuery(int PatientId, int PageNumber, int PageSize);

/// <summary>One appointment in a patient's history.</summary>
public sealed record PatientAppointmentHistoryItem(int AppointmentId, DateTime SlotStartUtc, int StatusId);
