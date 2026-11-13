namespace PatientManagement.Domain.Prescriptions;

/// <summary>
/// One time a prescription was printed, with the content that was printed (plan item 41). The snapshot is the serialized
/// prescription document, so a reprint shows exactly what was printed.
/// </summary>
public sealed class PrescriptionPrint
{
    private PrescriptionPrint(int prescriptionPrintId, int prescriptionId, DateTime printedAt, string snapshotJson)
    {
        PrescriptionPrintId = prescriptionPrintId;
        PrescriptionId = prescriptionId;
        PrintedAt = printedAt;
        SnapshotJson = snapshotJson;
    }

    public int PrescriptionPrintId { get; private set; }

    public int PrescriptionId { get; private set; }

    /// <summary>When the print was made, in UTC.</summary>
    public DateTime PrintedAt { get; private set; }

    /// <summary>The serialized prescription content as it was printed. Never changed after the print.</summary>
    public string SnapshotJson { get; private set; }

    /// <summary>Records a print of the prescription with the content that was printed.</summary>
    public static PrescriptionPrint Record(int prescriptionId, DateTime printedAt, string snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            throw new ArgumentException("A print must carry the content that was printed.", nameof(snapshotJson));
        }

        return new PrescriptionPrint(0, prescriptionId, printedAt, snapshotJson);
    }

    /// <summary>Rebuilds a print from a stored row.</summary>
    public static PrescriptionPrint Reconstitute(int prescriptionPrintId, int prescriptionId, DateTime printedAt, string snapshotJson) =>
        new(prescriptionPrintId, prescriptionId, printedAt, snapshotJson);

    /// <summary>Called by the repository after insert, when the database has assigned the id.</summary>
    public void AssignPrescriptionPrintId(int prescriptionPrintId) => PrescriptionPrintId = prescriptionPrintId;
}
