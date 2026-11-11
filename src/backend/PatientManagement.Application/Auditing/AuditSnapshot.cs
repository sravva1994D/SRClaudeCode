namespace PatientManagement.Application.Auditing;

/// <summary>
/// The values an audit row stores. Null means "not applicable" (no old values on a create, no new values on a delete).
/// For an update, only the fields whose values changed are present.
/// </summary>
public sealed record AuditSnapshot(
    IReadOnlyDictionary<string, object?>? OldValues,
    IReadOnlyDictionary<string, object?>? NewValues);

/// <summary>Turns an <see cref="AuditEntry"/> into the values to store (CLAUDE.md 6.4: changed fields only).</summary>
public static class AuditChanges
{
    public static AuditSnapshot Resolve(AuditEntry entry) => entry.Action switch
    {
        AuditAction.Create => new AuditSnapshot(null, entry.NewValues ?? EmptyValues),
        AuditAction.Delete => new AuditSnapshot(entry.OldValues ?? EmptyValues, null),
        _ => ResolveUpdate(entry.OldValues ?? EmptyValues, entry.NewValues ?? EmptyValues),
    };

    private static readonly IReadOnlyDictionary<string, object?> EmptyValues =
        new Dictionary<string, object?>(StringComparer.Ordinal);

    private static AuditSnapshot ResolveUpdate(
        IReadOnlyDictionary<string, object?> oldValues,
        IReadOnlyDictionary<string, object?> newValues)
    {
        var changedOld = new Dictionary<string, object?>(StringComparer.Ordinal);
        var changedNew = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var field in oldValues.Keys.Union(newValues.Keys, StringComparer.Ordinal))
        {
            var inOld = oldValues.TryGetValue(field, out var before);
            var inNew = newValues.TryGetValue(field, out var after);
            // A field that appears or disappears counts as a change, even when its value is null.
            if (inOld != inNew || !Equals(before, after))
            {
                changedOld[field] = before;
                changedNew[field] = after;
            }
        }

        return new AuditSnapshot(changedOld, changedNew);
    }
}
