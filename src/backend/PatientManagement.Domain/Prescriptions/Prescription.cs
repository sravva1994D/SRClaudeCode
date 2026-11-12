namespace PatientManagement.Domain.Prescriptions;

/// <summary>
/// The printable prescription issued for one completed visit (plan item 40). The content is not stored here: it is built
/// from the visit's current records when it is viewed.
/// </summary>
public sealed class Prescription
{
    private Prescription(int prescriptionId, int visitId, DateOnly prescriptionDate, bool isActive, byte[] rowVersion)
    {
        PrescriptionId = prescriptionId;
        VisitId = visitId;
        PrescriptionDate = prescriptionDate;
        IsActive = isActive;
        RowVersion = rowVersion;
    }

    public int PrescriptionId { get; private set; }

    public int VisitId { get; private set; }

    public DateOnly PrescriptionDate { get; private set; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; }

    /// <summary>Issues a prescription for a visit on the given date. The visit's own rules are checked by the caller.</summary>
    public static Prescription Issue(int visitId, DateOnly prescriptionDate) =>
        new(0, visitId, prescriptionDate, isActive: true, Array.Empty<byte>());

    /// <summary>Rebuilds a prescription from a stored row.</summary>
    public static Prescription Reconstitute(
        int prescriptionId,
        int visitId,
        DateOnly prescriptionDate,
        bool isActive,
        byte[] rowVersion) =>
        new(prescriptionId, visitId, prescriptionDate, isActive, rowVersion);

    /// <summary>Called by the repository after insert, when the database has assigned the id.</summary>
    public void AssignPrescriptionId(int prescriptionId) => PrescriptionId = prescriptionId;

    /// <summary>Called by the repository after insert, with the new row version.</summary>
    public void SetRowVersion(byte[] rowVersion) => RowVersion = rowVersion;

    /// <summary>Values written to the audit row.</summary>
    public IReadOnlyDictionary<string, object?> AuditValues() => new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["VisitId"] = VisitId,
        ["PrescriptionDate"] = PrescriptionDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        ["IsActive"] = IsActive,
    };
}
