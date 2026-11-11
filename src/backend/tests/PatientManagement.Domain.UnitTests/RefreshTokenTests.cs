using FluentAssertions;
using PatientManagement.Domain.Auth;

namespace PatientManagement.Domain.UnitTests;

public sealed class RefreshTokenTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Continue_NewToken_KeepsTheFamilyAndItsAbsoluteExpiry()
    {
        var previous = RefreshToken.Issue(1, Guid.NewGuid(), "hash-1", Now, Now.AddHours(8));

        var next = RefreshToken.Continue(previous, "hash-2", Now.AddMinutes(10));

        next.FamilyId.Should().Be(previous.FamilyId);
        next.ExpiresUtc.Should().Be(previous.ExpiresUtc);
        next.LastUsedUtc.Should().Be(Now.AddMinutes(10));
    }

    [Fact]
    public void IsIdleAt_UnusedForLongerThanTheLimit_ReturnsTrue()
    {
        var token = RefreshToken.Issue(1, Guid.NewGuid(), "hash", Now, Now.AddHours(8));

        token.IsIdleAt(Now.AddMinutes(31), TimeSpan.FromMinutes(30)).Should().BeTrue();
    }

    [Fact]
    public void IsIdleAt_UnusedForExactlyTheLimit_ReturnsFalse()
    {
        var token = RefreshToken.Issue(1, Guid.NewGuid(), "hash", Now, Now.AddHours(8));

        token.IsIdleAt(Now.AddMinutes(30), TimeSpan.FromMinutes(30)).Should().BeFalse();
    }

    [Fact]
    public void IsExpiredAt_AtTheAbsoluteLimit_ReturnsTrue()
    {
        var token = RefreshToken.Issue(1, Guid.NewGuid(), "hash", Now, Now.AddHours(8));

        token.IsExpiredAt(Now.AddHours(8)).Should().BeTrue();
    }

    [Fact]
    public void Revoke_AlreadyRevoked_KeepsTheFirstReason()
    {
        var token = RefreshToken.Issue(1, Guid.NewGuid(), "hash", Now, Now.AddHours(8));
        token.Revoke(Now, RefreshToken.RevocationReasons.Rotated);

        token.Revoke(Now.AddMinutes(1), RefreshToken.RevocationReasons.ReuseDetected);

        token.RevokedReason.Should().Be(RefreshToken.RevocationReasons.Rotated);
        token.RevokedUtc.Should().Be(Now);
    }
}
