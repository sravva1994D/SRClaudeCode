using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Auth.Handlers;
using PatientManagement.Domain.Auth;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Users;

namespace PatientManagement.Application.UnitTests;

public sealed class LoginCommandHandlerTests
{
    private readonly AuthDependencies _dependencies = new();

    [Fact]
    public async Task HandleAsync_CorrectPassword_ReturnsSessionAndStartsRefreshFamily()
    {
        _dependencies.Users.FindActiveByUsernameAsync("dev.doctor", Arg.Any<CancellationToken>())
            .Returns(AuthTestData.Account());
        var handler = CreateHandler();

        var session = await handler.HandleAsync(new LoginCommand("dev.doctor", AuthTestData.Password), CancellationToken.None);

        session.RefreshToken.Should().Be(AuthTestData.PlainRefreshToken);
        session.RefreshTokenExpiresUtc.Should().Be(AuthTestData.Now.AddHours(8));
        await _dependencies.RefreshTokens.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_RecordsFailureAndThrowsGenericError()
    {
        var account = AuthTestData.Account();
        _dependencies.Users.FindActiveByUsernameAsync("dev.doctor", Arg.Any<CancellationToken>()).Returns(account);
        _dependencies.PasswordHasher.Verify("wrong", AuthTestData.PasswordHash).Returns(false);
        var handler = CreateHandler();

        var act = () => handler.HandleAsync(new LoginCommand("dev.doctor", "wrong"), CancellationToken.None);

        var error = await act.Should().ThrowAsync<AuthenticationFailedException>();
        error.Which.ErrorCode.Should().Be(AuthMessages.InvalidCredentialsCode);
        error.Which.Message.Should().Be(AuthMessages.InvalidCredentialsMessage);
        account.FailedLoginCount.Should().Be(1);
        await _dependencies.Users.Received(1).SaveAsync(account, Arg.Any<IReadOnlyDictionary<string, object?>>(), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_FifthFailureInWindow_LocksTheAccount()
    {
        var account = AuthTestData.Account(
            failedLoginCount: 4,
            failedLoginWindowStartUtc: AuthTestData.Now.AddMinutes(-2));
        _dependencies.Users.FindActiveByUsernameAsync("dev.doctor", Arg.Any<CancellationToken>()).Returns(account);
        _dependencies.PasswordHasher.Verify("wrong", AuthTestData.PasswordHash).Returns(false);
        var handler = CreateHandler();

        var act = () => handler.HandleAsync(new LoginCommand("dev.doctor", "wrong"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        account.LockedUntilUtc.Should().Be(AuthTestData.Now.Add(LoginLockoutRules.LockDuration));
    }

    [Fact]
    public async Task HandleAsync_CorrectPasswordWhileLocked_ThrowsGenericErrorAndStartsNoSession()
    {
        var account = AuthTestData.Account(lockedUntilUtc: AuthTestData.Now.AddMinutes(5));
        _dependencies.Users.FindActiveByUsernameAsync("dev.doctor", Arg.Any<CancellationToken>()).Returns(account);
        var handler = CreateHandler();

        var act = () => handler.HandleAsync(new LoginCommand("dev.doctor", AuthTestData.Password), CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationFailedException>();
        _dependencies.PasswordHasher.Received(1).VerifyAgainstDummy(AuthTestData.Password);
        await _dependencies.RefreshTokens.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_UnknownUser_ThrowsGenericErrorAndSpendsTheSameWork()
    {
        _dependencies.Users.FindActiveByUsernameAsync("nobody", Arg.Any<CancellationToken>()).Returns((UserAccount?)null);
        var handler = CreateHandler();

        var act = () => handler.HandleAsync(new LoginCommand("nobody", AuthTestData.Password), CancellationToken.None);

        var error = await act.Should().ThrowAsync<AuthenticationFailedException>();
        error.Which.Message.Should().Be(AuthMessages.InvalidCredentialsMessage);
        _dependencies.PasswordHasher.Received(1).VerifyAgainstDummy(AuthTestData.Password);
    }

    private LoginCommandHandler CreateHandler() => new(
        new InlineUnitOfWork(),
        _dependencies.Users,
        _dependencies.RefreshTokens,
        _dependencies.PasswordHasher,
        _dependencies.Generator,
        _dependencies.AccessTokenIssuer,
        _dependencies.Clock,
        AuthTestData.OptionsWrapper());
}
