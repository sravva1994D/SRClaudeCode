using PatientManagement.Domain.Visits;

namespace PatientManagement.Application.Visits;

/// <summary>Starts a visit from an appointment. The appointment must be scheduled and must have no active visit yet.</summary>
public sealed record StartVisitCommand(int AppointmentId, int ChangedByUserId);

public sealed record GetVisitQuery(int VisitId);

/// <summary>What the API returns for a visit.</summary>
public sealed record VisitDetails(int VisitId, int PatientId, int? AppointmentId, int StatusId)
{
    public static VisitDetails From(Visit visit) => new(visit.VisitId, visit.PatientId, visit.AppointmentId, visit.StatusId);
}
