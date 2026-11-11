namespace PatientManagement.Application.Auditing;

/// <summary>
/// One data change to record. Values are column name to value. Create uses only <see cref="NewValues"/>, Delete uses
/// only <see cref="OldValues"/>, and Update needs both. Values must never be logged, only written to the audit row.
/// </summary>
public sealed record AuditEntry(
    string TableName,
    string RecordId,
    AuditAction Action,
    IReadOnlyDictionary<string, object?>? OldValues,
    IReadOnlyDictionary<string, object?>? NewValues,
    int? UserId);
