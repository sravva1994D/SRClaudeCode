using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Domain.Visits;

/// <summary>Visit status ids, matching the seeded VisitStatuses table (migration V0014).</summary>
public static class VisitStatuses
{
    public const int InProgress = 1;
    public const int Completed = 2;
}

/// <summary>
/// One consultation with a patient. It can start from an appointment, in which case <see cref="AppointmentId"/> is set;
/// a walk-in visit has no appointment.
/// </summary>
public sealed class Visit
{
    private Visit(
        int visitId,
        int patientId,
        int? appointmentId,
        int statusId,
        bool isActive,
        byte[] rowVersion)
    {
        VisitId = visitId;
        PatientId = patientId;
        AppointmentId = appointmentId;
        StatusId = statusId;
        IsActive = isActive;
        RowVersion = rowVersion;
    }

    public int VisitId { get; private set; }

    public int PatientId { get; private set; }

    public int? AppointmentId { get; private set; }

    public int StatusId { get; private set; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; }

    /// <summary>
    /// Vitals are recorded only while the visit is in progress. A completed visit is changed through the edit rules
    /// instead (plan Sprint 7–8, item 38).
    /// </summary>
    public void EnsureInProgress()
    {
        if (StatusId != VisitStatuses.InProgress)
        {
            throw new DomainException("visit_not_in_progress", "This visit is no longer in progress.");
        }
    }

    /// <summary>Starts a visit in progress. The appointment's own rules are checked by the appointment first.</summary>
    public static Visit Start(int patientId, int appointmentId) =>
        new(0, patientId, appointmentId, VisitStatuses.InProgress, isActive: true, Array.Empty<byte>());

    /// <summary>Rebuilds a visit from a stored row.</summary>
    public static Visit Reconstitute(
        int visitId,
        int patientId,
        int? appointmentId,
        int statusId,
        bool isActive,
        byte[] rowVersion) =>
        new(visitId, patientId, appointmentId, statusId, isActive, rowVersion);

    /// <summary>Called by the repository after insert, when the database has assigned the id.</summary>
    public void AssignVisitId(int visitId) => VisitId = visitId;

    /// <summary>Called by the repository after each save, with the new row version.</summary>
    public void SetRowVersion(byte[] rowVersion) => RowVersion = rowVersion;

    /// <summary>Values written to the audit row.</summary>
    public IReadOnlyDictionary<string, object?> AuditValues() => new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["PatientId"] = PatientId,
        ["AppointmentId"] = AppointmentId,
        ["VisitStatusId"] = StatusId,
        ["IsActive"] = IsActive,
    };
}
