namespace PatientManagement.Application.Auditing;

/// <summary>The data change an audit row records (CLAUDE.md 6.4).</summary>
public enum AuditAction
{
    Create,
    Update,
    Delete,
}
