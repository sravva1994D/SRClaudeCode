using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Domain.Consultation;

/// <summary>
/// One medicine prescribed in a visit. Name, dosage, frequency and duration are required; special instructions are
/// optional. A draft save replaces a visit's medications as a whole, so a medicine is never edited in place.
/// </summary>
public sealed class Medication
{
    private Medication(
        int medicationId,
        int visitId,
        string medicineName,
        string dosage,
        string frequency,
        string duration,
        string? specialInstructions,
        int sortOrder,
        bool isActive,
        byte[] rowVersion)
    {
        MedicationId = medicationId;
        VisitId = visitId;
        MedicineName = medicineName;
        Dosage = dosage;
        Frequency = frequency;
        Duration = duration;
        SpecialInstructions = specialInstructions;
        SortOrder = sortOrder;
        IsActive = isActive;
        RowVersion = rowVersion;
    }

    public int MedicationId { get; private set; }

    public int VisitId { get; }

    public string MedicineName { get; }

    public string Dosage { get; }

    public string Frequency { get; }

    public string Duration { get; }

    /// <summary>The optional instruction, such as "After food"; null when none was entered.</summary>
    public string? SpecialInstructions { get; }

    public int SortOrder { get; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; }

    /// <summary>
    /// Creates a medication. Every text is trimmed. A required field that is blank after trimming is refused; blank
    /// instructions are stored as none.
    /// </summary>
    public static Medication Record(
        int visitId,
        string medicineName,
        string dosage,
        string frequency,
        string duration,
        string? specialInstructions,
        int sortOrder)
    {
        var name = RequiredText(medicineName, "medication_name_required", "A medicine name is required.");
        var trimmedDosage = RequiredText(dosage, "medication_dosage_required", "A dosage is required.");
        var trimmedFrequency = RequiredText(frequency, "medication_frequency_required", "A frequency is required.");
        var trimmedDuration = RequiredText(duration, "medication_duration_required", "A duration is required.");
        var instructions = specialInstructions?.Trim();

        return new Medication(
            0,
            visitId,
            name,
            trimmedDosage,
            trimmedFrequency,
            trimmedDuration,
            string.IsNullOrEmpty(instructions) ? null : instructions,
            sortOrder,
            isActive: true,
            Array.Empty<byte>());
    }

    /// <summary>Rebuilds a medication from a stored row.</summary>
    public static Medication Reconstitute(
        int medicationId,
        int visitId,
        string medicineName,
        string dosage,
        string frequency,
        string duration,
        string? specialInstructions,
        int sortOrder,
        bool isActive,
        byte[] rowVersion) =>
        new(
            medicationId,
            visitId,
            medicineName,
            dosage,
            frequency,
            duration,
            specialInstructions,
            sortOrder,
            isActive,
            rowVersion);

    /// <summary>Marks the medication as replaced by a newer draft. The row stays in the table for the audit trail.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Called by the repository after insert, when the database has assigned the id.</summary>
    public void AssignMedicationId(int medicationId) => MedicationId = medicationId;

    /// <summary>Called by the repository after each save, with the new row version.</summary>
    public void SetRowVersion(byte[] rowVersion) => RowVersion = rowVersion;

    /// <summary>Values written to the audit row.</summary>
    public IReadOnlyDictionary<string, object?> AuditValues() => new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["VisitId"] = VisitId,
        ["MedicineName"] = MedicineName,
        ["Dosage"] = Dosage,
        ["Frequency"] = Frequency,
        ["Duration"] = Duration,
        ["SpecialInstructions"] = SpecialInstructions,
        ["SortOrder"] = SortOrder,
        ["IsActive"] = IsActive,
    };

    private static string RequiredText(string? value, string errorCode, string message)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new DomainException(errorCode, message);
        }

        return trimmed;
    }
}
