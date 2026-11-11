using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Auth.Handlers;
using PatientManagement.Domain.Auth;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Application.UnitTests;

public sealed class RefreshSessionCommandHandlerTests
{
    private readonly AuthDependencies _dependencies = new();

    [Fact]
    public async Task HandleAsync_ActiveToken_RotatesAndRevokesThePresentedToken()
    {
        var presented = AuthTestData.Token();
        _dependencies.RefreshTokens.FindByHashAsync(AuthTestData.RefreshTokenHash, Arg.Any<CancellationToken>())
            .Returns(presented);
        _dependencies.Users.FindActiveByIdAsync(1, Arg.Any<CancellationToken>()).Returns(AuthTestData.Account());

        var session = await CreateHandler().HandleAsync(new RefreshSessionCommand("presented"), CancellationToken.None);

        presented.IsRevoked.Should().BeTrue();
        presented.RevokedReason.Should().Be(RefreshToken.RevocationReasons.Rotated);
        session.RefreshToken.Should().Be(AuthTestData.PlainRefreshToken);
        session.RefreshTokenExpiresUtc.Should().Be(presented.ExpiresUtc, "the family keeps its absolute expiry");
    }

    [Fact]
    public async Task HandleAsync_RotatedTokenPresentedAgain_RevokesTheWholeFamilyAndThrows()
    {
        var reused = AuthTestData.Token(revokedReason: RefreshToken.RevocationReasons.Rotated);
        _dependencies.RefreshTokens.FindByHashAsync(AuthTestData.RefreshTokenHash, Arg.Any<CancellationToken>())
            .Returns(reused);

        var act = () => CreateHandler().HandleAsync(new RefreshSessionCommand("copied"), CancellationToken.None);

        var error = await act.Should().ThrowAsync<AuthenticationFailedException>();
        error.Which.Message.Should().Be(AuthMessages.SessionEndedMessage);
        await _dependencies.RefreshTokens.Received(1).RevokeFamilyAsync(
            reused.FamilyId,
            RefreshToken.RevocationReasons.ReuseDetected,
            AuthTestData.Now,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownToken_ThrowsWithoutChangingAnything()
    {
        _dependencies.RefreshTokens.FindByHashAsync(AuthTestData.RefreshTokenHash, Arg.Any<CancellationToken>())
            .Returns((RefreshToken?)null);

        var act = () => CreateHandler().HandleAsync(new RefreshSessionCommand("unknown"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        await _dependencies.RefreshTokens.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, default);
    }

    [Fact]
    public async Task HandleAsync_TokenPastAbsoluteExpiry_RevokesItAndThrows()
    {
        var expired = AuthTestData.Token(expiresUtc: AuthTestData.Now);
        _dependencies.RefreshTokens.FindByHashAsync(AuthTestData.RefreshTokenHash, Arg.Any<CancellationToken>())
            .Returns(expired);

        var act = () => CreateHandler().HandleAsync(new RefreshSessionCommand("old"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        expired.RevokedReason.Should().Be(RefreshToken.RevocationReasons.Expired);
        await _dependencies.RefreshTokens.Received(1).SaveAsync(expired, Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnusedForMoreThanThirtyMinutes_RevokesItAndThrows()
    {
        var idle = AuthTestData.Token(lastUsedUtc: AuthTestData.Now.AddMinutes(-31));
        _dependencies.RefreshTokens.FindByHashAsync(AuthTestData.RefreshTokenHash, Arg.Any<CancellationToken>())
            .Returns(idle);

        var act = () => CreateHandler().HandleAsync(new RefreshSessionCommand("idle"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        idle.RevokedReason.Should().Be(RefreshToken.RevocationReasons.IdleTimeout);
    }

    [Fact]
    public async Task HandleAsync_UnusedForExactlyThirtyMinutes_StillRotates()
    {
        var atLimit = AuthTestData.Token(lastUsedUtc: AuthTestData.Now.AddMinutes(-30));
        _dependencies.RefreshTokens.FindByHashAsync(AuthTestData.RefreshTokenHash, Arg.Any<CancellationToken>())
            .Returns(atLimit);
        _dependencies.Users.FindActiveByIdAsync(1, Arg.Any<CancellationToken>()).Returns(AuthTestData.Account());

        var session = await CreateHandler().HandleAsync(new RefreshSessionCommand("recent"), CancellationToken.None);

        session.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HandleAsync_AccountNoLongerActive_RevokesItAndThrows()
    {
        var presented = AuthTestData.Token();
        _dependencies.RefreshTokens.FindByHashAsync(AuthTestData.RefreshTokenHash, Arg.Any<CancellationToken>())
            .Returns(presented);
        _dependencies.Users.FindActiveByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Domain.Users.UserAccount?)null);

        var act = () => CreateHandler().HandleAsync(new RefreshSessionCommand("presented"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        presented.RevokedReason.Should().Be(RefreshToken.RevocationReasons.AccountInactive);
    }

    private RefreshSessionCommandHandler CreateHandler() => new(
        new InlineUnitOfWork(),
        _dependencies.Users,
        _dependencies.RefreshTokens,
        _dependencies.Generator,
        _dependencies.AccessTokenIssuer,
        _dependencies.Clock,
        AuthTestData.OptionsWrapper());
}
