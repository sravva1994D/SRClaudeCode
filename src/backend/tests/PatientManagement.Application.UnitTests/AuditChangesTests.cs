using FluentAssertions;
using PatientManagement.Application.Auditing;

namespace PatientManagement.Application.UnitTests;

public sealed class AuditChangesTests
{
    [Fact]
    public void Resolve_Create_ReturnsNewValuesOnly()
    {
        var entry = new AuditEntry(
            "Users", "7", AuditAction.Create, null, Values(("Username", "doctor")), UserId: null);

        var snapshot = AuditChanges.Resolve(entry);

        snapshot.OldValues.Should().BeNull();
        snapshot.NewValues.Should().BeEquivalentTo(Values(("Username", "doctor")));
    }

    [Fact]
    public void Resolve_Delete_ReturnsOldValuesOnly()
    {
        var entry = new AuditEntry(
            "Users", "7", AuditAction.Delete, Values(("IsActive", true)), null, UserId: 1);

        var snapshot = AuditChanges.Resolve(entry);

        snapshot.OldValues.Should().BeEquivalentTo(Values(("IsActive", true)));
        snapshot.NewValues.Should().BeNull();
    }

    [Fact]
    public void Resolve_Update_ReturnsOnlyFieldsThatChanged()
    {
        var entry = new AuditEntry(
            "Users",
            "7",
            AuditAction.Update,
            Values(("FailedLoginCount", 0), ("Username", "doctor")),
            Values(("FailedLoginCount", 1), ("Username", "doctor")),
            UserId: 1);

        var snapshot = AuditChanges.Resolve(entry);

        snapshot.OldValues.Should().BeEquivalentTo(Values(("FailedLoginCount", 0)));
        snapshot.NewValues.Should().BeEquivalentTo(Values(("FailedLoginCount", 1)));
    }

    [Fact]
    public void Resolve_UpdateWithNoChanges_ReturnsEmptyValues()
    {
        var entry = new AuditEntry(
            "Users", "7", AuditAction.Update, Values(("Username", "doctor")), Values(("Username", "doctor")), UserId: 1);

        var snapshot = AuditChanges.Resolve(entry);

        snapshot.OldValues.Should().BeEmpty();
        snapshot.NewValues.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_UpdateWithFieldAddedOrRemoved_IncludesThatField()
    {
        var entry = new AuditEntry(
            "Users",
            "7",
            AuditAction.Update,
            Values(("LockedUntilDate", null)),
            Values(("LastLoginDate", "2026-11-11")),
            UserId: 1);

        var snapshot = AuditChanges.Resolve(entry);

        snapshot.OldValues.Should().ContainKey("LockedUntilDate");
        snapshot.NewValues.Should().ContainKey("LastLoginDate");
    }

    private static IReadOnlyDictionary<string, object?> Values(params (string Field, object? Value)[] fields) =>
        fields.ToDictionary(field => field.Field, field => field.Value, StringComparer.Ordinal);
}
