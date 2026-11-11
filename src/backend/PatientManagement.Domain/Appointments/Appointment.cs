using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Domain.Appointments;

/// <summary>Appointment status ids, matching the seeded AppointmentStatuses table (migration V0002).</summary>
public static class AppointmentStatuses
{
    public const int Scheduled = 1;
    public const int Completed = 2;
    public const int Cancelled = 3;
    public const int NoShow = 4;
}

/// <summary>
/// An appointment for one patient in one fixed 15-minute slot. Slots start on the quarter hour, and only future slots
/// can be booked (plan Sprint 5–6, item 25).
/// </summary>
public sealed class Appointment
{
    public const int SlotMinutes = 15;

    private Appointment(
        int appointmentId,
        int patientId,
        DateTime slotStartUtc,
        int statusId,
        bool isActive,
        byte[] rowVersion)
    {
        AppointmentId = appointmentId;
        PatientId = patientId;
        SlotStartUtc = slotStartUtc;
        StatusId = statusId;
        IsActive = isActive;
        RowVersion = rowVersion;
    }

    public int AppointmentId { get; private set; }

    public int PatientId { get; private set; }

    /// <summary>Start of the slot, in UTC.</summary>
    public DateTime SlotStartUtc { get; private set; }

    public int StatusId { get; private set; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; }

    /// <summary>
    /// Books a slot. The slot must start on a 15-minute boundary and must still be in the future. Two appointments
    /// cannot share a slot; the database enforces that, and the repository reports it as a slot conflict.
    /// </summary>
    public static Appointment Schedule(int patientId, DateTime slotStartUtc, DateTime nowUtc)
    {
        EnsureOnQuarterHour(slotStartUtc);
        if (slotStartUtc <= nowUtc)
        {
            throw new DomainException("appointment_in_past", "An appointment must be booked for a future time.");
        }

        return new Appointment(0, patientId, slotStartUtc, AppointmentStatuses.Scheduled, isActive: true, Array.Empty<byte>());
    }

    /// <summary>Rebuilds an appointment from a stored row.</summary>
    public static Appointment Reconstitute(
        int appointmentId,
        int patientId,
        DateTime slotStartUtc,
        int statusId,
        bool isActive,
        byte[] rowVersion) =>
        new(appointmentId, patientId, slotStartUtc, statusId, isActive, rowVersion);

    /// <summary>Called by the repository after insert, when the database has assigned the id.</summary>
    public void AssignAppointmentId(int appointmentId) => AppointmentId = appointmentId;

    /// <summary>Called by the repository after each save, with the new row version.</summary>
    public void SetRowVersion(byte[] rowVersion) => RowVersion = rowVersion;

    /// <summary>Values written to the audit row.</summary>
    public IReadOnlyDictionary<string, object?> AuditValues() => new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["PatientId"] = PatientId,
        ["SlotStart"] = SlotStartUtc.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
        ["AppointmentStatusId"] = StatusId,
        ["IsActive"] = IsActive,
    };

    private static void EnsureOnQuarterHour(DateTime slotStartUtc)
    {
        var isOnBoundary = slotStartUtc.Minute % SlotMinutes == 0
            && slotStartUtc.Second == 0
            && slotStartUtc.Millisecond == 0;
        if (!isOnBoundary)
        {
            throw new DomainException("slot_not_aligned", "Appointments start every 15 minutes, on the quarter hour.");
        }
    }
}
