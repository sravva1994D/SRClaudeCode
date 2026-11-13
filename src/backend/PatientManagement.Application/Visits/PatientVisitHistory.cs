namespace PatientManagement.Application.Visits;

/// <summary>
/// One page of a patient's visits, newest first. <see cref="From"/> and <see cref="To"/> are inclusive calendar days in UTC
/// and are optional; a null bound leaves that side open.
/// </summary>
public sealed record PatientVisitHistoryQuery(
    int PatientId,
    DateOnly? From,
    DateOnly? To,
    int PageNumber,
    int PageSize);

/// <summary>One visit in a patient's history: when it started and where it stands now, without the clinical content.</summary>
public sealed record PatientVisitHistoryItem(
    int VisitId,
    DateTime StartedAt,
    int StatusId,
    DateTime ModifiedDate,
    int? AppointmentId);
