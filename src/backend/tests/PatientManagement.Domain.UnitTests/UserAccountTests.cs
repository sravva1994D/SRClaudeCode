using FluentAssertions;
using PatientManagement.Domain.Users;

namespace PatientManagement.Domain.UnitTests;

public sealed class UserAccountTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RecordFailedLogin_FirstFailure_StartsAWindowWithCountOne()
    {
        var account = CreateAccount();

        account.RecordFailedLogin(Now);

        account.FailedLoginCount.Should().Be(1);
        account.FailedLoginWindowStartUtc.Should().Be(Now);
        account.LockedUntilUtc.Should().BeNull();
    }

    [Fact]
    public void RecordFailedLogin_FifthFailureInsideWindow_LocksForFifteenMinutes()
    {
        var account = CreateAccount(failedLoginCount: 4, windowStart: Now.AddMinutes(-14));

        account.RecordFailedLogin(Now);

        account.LockedUntilUtc.Should().Be(Now.Add(LoginLockoutRules.LockDuration));
        account.FailedLoginCount.Should().Be(0);
        account.FailedLoginWindowStartUtc.Should().BeNull();
    }

    [Fact]
    public void RecordFailedLogin_FailureAfterWindowExpired_RestartsTheCount()
    {
        var account = CreateAccount(failedLoginCount: 4, windowStart: Now.AddMinutes(-16));

        account.RecordFailedLogin(Now);

        account.FailedLoginCount.Should().Be(1);
        account.LockedUntilUtc.Should().BeNull();
    }

    [Fact]
    public void RecordSuccessfulLogin_AfterFailures_ClearsCountAndWindow()
    {
        var account = CreateAccount(failedLoginCount: 3, windowStart: Now.AddMinutes(-1));

        account.RecordSuccessfulLogin(Now);

        account.FailedLoginCount.Should().Be(0);
        account.FailedLoginWindowStartUtc.Should().BeNull();
        account.LastLoginUtc.Should().Be(Now);
    }

    [Fact]
    public void IsLockedAt_BeforeLockEnds_ReturnsTrue()
    {
        var account = CreateAccount(lockedUntil: Now.AddMinutes(1));

        account.IsLockedAt(Now).Should().BeTrue();
    }

    [Fact]
    public void IsLockedAt_AfterLockEnds_ReturnsFalse()
    {
        var account = CreateAccount(lockedUntil: Now.AddMinutes(-1));

        account.IsLockedAt(Now).Should().BeFalse();
    }

    [Fact]
    public void AuditValues_PasswordHash_IsRedacted()
    {
        var account = CreateAccount();

        account.AuditValues()["PasswordHash"].Should().Be("[redacted]");
    }

    private static UserAccount CreateAccount(
        int failedLoginCount = 0,
        DateTime? windowStart = null,
        DateTime? lockedUntil = null) =>
        UserAccount.Reconstitute(
            userId: 1,
            username: "dev.doctor",
            passwordHash: "hash",
            isActive: true,
            failedLoginCount: failedLoginCount,
            failedLoginWindowStartUtc: windowStart,
            lockedUntilUtc: lockedUntil,
            lastLoginUtc: null,
            passwordChangedUtc: null,
            rowVersion: [1]);
}
