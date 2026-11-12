namespace PatientManagement.Application.Visits;

/// <summary>
/// Completes an in-progress visit. Vitals must be recorded; the visit's appointment (when it has one) becomes Completed.
/// Everything happens in one transaction (plan Sprint 7–8, item 36).
/// </summary>
public sealed record CompleteVisitCommand(int VisitId, int ChangedByUserId);
