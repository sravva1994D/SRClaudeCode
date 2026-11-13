namespace PatientManagement.Application.Prescriptions;

/// <summary>Records a print of the visit's prescription, with a snapshot of the content as it is printed.</summary>
public sealed record RecordPrescriptionPrintCommand(int VisitId, int ChangedByUserId);

public sealed record GetPrescriptionPrintsQuery(int VisitId);

public sealed record GetPrescriptionPrintQuery(int VisitId, int PrescriptionPrintId);

/// <summary>One print in the reprint list: when it was printed, without the content.</summary>
public sealed record PrescriptionPrintSummary(int PrescriptionPrintId, DateTime PrintedAt);

/// <summary>A print with the content that was printed. A reprint returns this from the snapshot, not from current records.</summary>
public sealed record PrescriptionPrintDetails(int PrescriptionPrintId, DateTime PrintedAt, PrescriptionDocument Document);
