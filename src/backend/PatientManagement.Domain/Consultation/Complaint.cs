using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Domain.Consultation;

/// <summary>
/// One complaint (symptom) the patient reported in a visit. A draft save replaces a visit's complaints as a whole: the
/// old rows are deactivated and new ones are inserted, so a complaint is never edited in place.
/// </summary>
public sealed class Complaint
{
    private Complaint(int complaintId, int visitId, string text, int sortOrder, bool isActive, byte[] rowVersion)
    {
        ComplaintId = complaintId;
        VisitId = visitId;
        Text = text;
        SortOrder = sortOrder;
        IsActive = isActive;
        RowVersion = rowVersion;
    }

    public int ComplaintId { get; private set; }

    public int VisitId { get; }

    public string Text { get; }

    public int SortOrder { get; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; }

    /// <summary>Creates a complaint. Surrounding spaces are removed; text that is blank after trimming is refused.</summary>
    public static Complaint Record(int visitId, string text, int sortOrder)
    {
        var trimmed = (text ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new DomainException("complaint_blank", "A complaint cannot be blank.");
        }

        return new Complaint(0, visitId, trimmed, sortOrder, isActive: true, Array.Empty<byte>());
    }

    /// <summary>Rebuilds a complaint from a stored row.</summary>
    public static Complaint Reconstitute(
        int complaintId,
        int visitId,
        string text,
        int sortOrder,
        bool isActive,
        byte[] rowVersion) =>
        new(complaintId, visitId, text, sortOrder, isActive, rowVersion);

    /// <summary>Marks the complaint as replaced by a newer draft. The row stays in the table for the audit trail.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Called by the repository after insert, when the database has assigned the id.</summary>
    public void AssignComplaintId(int complaintId) => ComplaintId = complaintId;

    /// <summary>Called by the repository after each save, with the new row version.</summary>
    public void SetRowVersion(byte[] rowVersion) => RowVersion = rowVersion;

    /// <summary>Values written to the audit row.</summary>
    public IReadOnlyDictionary<string, object?> AuditValues() => new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["VisitId"] = VisitId,
        ["ComplaintText"] = Text,
        ["SortOrder"] = SortOrder,
        ["IsActive"] = IsActive,
    };
}
