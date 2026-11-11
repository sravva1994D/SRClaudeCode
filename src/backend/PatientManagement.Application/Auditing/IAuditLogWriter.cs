using PatientManagement.Application.Auditing;

namespace PatientManagement.Application.Interfaces;

/// <summary>Appends one audit row. Must be called inside a unit of work so the row commits with the change.</summary>
public interface IAuditLogWriter
{
    Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken);
}
