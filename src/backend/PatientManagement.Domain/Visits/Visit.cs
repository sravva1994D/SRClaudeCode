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
        byte[] rowVersion,
        DateTime modifiedDate)
    {
        VisitId = visitId;
        PatientId = patientId;
        AppointmentId = appointmentId;
        StatusId = statusId;
        IsActive = isActive;
        RowVersion = rowVersion;
        ModifiedDate = modifiedDate;
    }

    public int VisitId { get; private set; }

    public int PatientId { get; private set; }

    public int? AppointmentId { get; private set; }

    public int StatusId { get; private set; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; }

    /// <summary>When the visit or any of its records last changed, in UTC (plan item 38).</summary>
    public DateTime ModifiedDate { get; private set; }

    /// <summary>
    /// Vitals are recorded only while the visit is in progress. Notes, medications and vitals corrections are allowed
    /// after completion (Q2), so only the recording and completion steps call this.
    /// </summary>
    public void EnsureInProgress()
    {
        if (StatusId != VisitStatuses.InProgress)
        {
            throw new DomainException("visit_not_in_progress", "This visit is no longer in progress.");
        }
    }

    /// <summary>
    /// Completes an in-progress visit. The caller checks the visit's required records (vitals) and completes the
    /// appointment in the same unit of work, so a failure at any step leaves the visit in progress.
    /// </summary>
    public void Complete()
    {
        EnsureInProgress();
        StatusId = VisitStatuses.Completed;
    }

    /// <summary>Starts a visit in progress. The appointment's own rules are checked by the appointment first.</summary>
    public static Visit Start(int patientId, int appointmentId) =>
        new(0, patientId, appointmentId, VisitStatuses.InProgress, isActive: true, Array.Empty<byte>(), default);

    /// <summary>Rebuilds a visit from a stored row.</summary>
    public static Visit Reconstitute(
        int visitId,
        int patientId,
        int? appointmentId,
        int statusId,
        bool isActive,
        byte[] rowVersion,
        DateTime modifiedDate = default) =>
        new(visitId, patientId, appointmentId, statusId, isActive, rowVersion, modifiedDate);

    /// <summary>Called by the repository after each save, with the time the save was made.</summary>
    public void SetModifiedDate(DateTime modifiedDate) => ModifiedDate = modifiedDate;

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
