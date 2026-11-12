using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Domain.Consultation;

/// <summary>
/// One diagnosis entry for a visit: the doctor's notes and an optional reference code. Like complaints, a draft save
/// replaces the visit's diagnoses as a whole.
/// </summary>
public sealed class Diagnosis
{
    private Diagnosis(
        int diagnosisId,
        int visitId,
        string text,
        string? code,
        int sortOrder,
        bool isActive,
        byte[] rowVersion)
    {
        DiagnosisId = diagnosisId;
        VisitId = visitId;
        Text = text;
        Code = code;
        SortOrder = sortOrder;
        IsActive = isActive;
        RowVersion = rowVersion;
    }

    public int DiagnosisId { get; private set; }

    public int VisitId { get; }

    public string Text { get; }

    /// <summary>The optional reference code, or null when none was entered.</summary>
    public string? Code { get; }

    public int SortOrder { get; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; }

    /// <summary>
    /// Creates a diagnosis. Text and code are trimmed; a code that is blank after trimming is stored as no code. Text that
    /// is blank after trimming is refused.
    /// </summary>
    public static Diagnosis Record(int visitId, string text, string? code, int sortOrder)
    {
        var trimmedText = (text ?? string.Empty).Trim();
        if (trimmedText.Length == 0)
        {
            throw new DomainException("diagnosis_blank", "A diagnosis cannot be blank.");
        }

        var trimmedCode = code?.Trim();
        return new Diagnosis(
            0,
            visitId,
            trimmedText,
            string.IsNullOrEmpty(trimmedCode) ? null : trimmedCode,
            sortOrder,
            isActive: true,
            Array.Empty<byte>());
    }

    /// <summary>Rebuilds a diagnosis from a stored row.</summary>
    public static Diagnosis Reconstitute(
        int diagnosisId,
        int visitId,
        string text,
        string? code,
        int sortOrder,
        bool isActive,
        byte[] rowVersion) =>
        new(diagnosisId, visitId, text, code, sortOrder, isActive, rowVersion);

    /// <summary>Marks the diagnosis as replaced by a newer draft. The row stays in the table for the audit trail.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Called by the repository after insert, when the database has assigned the id.</summary>
    public void AssignDiagnosisId(int diagnosisId) => DiagnosisId = diagnosisId;

    /// <summary>Called by the repository after each save, with the new row version.</summary>
    public void SetRowVersion(byte[] rowVersion) => RowVersion = rowVersion;

    /// <summary>Values written to the audit row.</summary>
    public IReadOnlyDictionary<string, object?> AuditValues() => new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["VisitId"] = VisitId,
        ["DiagnosisText"] = Text,
        ["DiagnosisCode"] = Code,
        ["SortOrder"] = SortOrder,
        ["IsActive"] = IsActive,
    };
}
