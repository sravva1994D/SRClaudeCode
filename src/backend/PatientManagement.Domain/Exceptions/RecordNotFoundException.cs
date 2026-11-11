namespace PatientManagement.Domain.Exceptions;

/// <summary>The record does not exist or is inactive. The API maps it to 404 (CLAUDE.md 6.5, 10.3).</summary>
public sealed class RecordNotFoundException(string recordType)
    : DomainException("record_not_found", $"{recordType} was not found.")
{
}
