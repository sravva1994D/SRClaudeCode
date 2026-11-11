namespace PatientManagement.Domain.Exceptions;

/// <summary>An update found the row changed since it was read (CLAUDE.md 6.3). The API maps it to 409.</summary>
public sealed class ConcurrencyConflictException(string recordName)
    : DomainException("concurrency_conflict", "This record was changed by someone else. Reload it and try again.")
{
    /// <summary>Which kind of record conflicted, for logs. Never includes identifying values.</summary>
    public string RecordName { get; } = recordName;
}
