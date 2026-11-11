using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Auth.Handlers;
using PatientManagement.Domain.Auth;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Application.UnitTests;

public sealed class AccountCommandHandlerTests
{
    private const string NewPassword = "NewPassw0rd!";

    private readonly AuthDependencies _dependencies = new();

    [Fact]
    public async Task LogoutHandleAsync_ActiveToken_RevokesItWithLogoutReason()
    {
        var presented = AuthTestData.Token();
        _dependencies.RefreshTokens.FindByHashAsync(AuthTestData.RefreshTokenHash, Arg.Any<CancellationToken>())
            .Returns(presented);

        await CreateLogoutHandler().HandleAsync(new LogoutCommand("presented"), CancellationToken.None);

        presented.RevokedReason.Should().Be(RefreshToken.RevocationReasons.Logout);
        await _dependencies.RefreshTokens.Received(1).SaveAsync(presented, Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogoutHandleAsync_AlreadyRevokedToken_ChangesNothing()
    {
        var revoked = AuthTestData.Token(revokedReason: RefreshToken.RevocationReasons.Logout);
        _dependencies.RefreshTokens.FindByHashAsync(AuthTestData.RefreshTokenHash, Arg.Any<CancellationToken>())
            .Returns(revoked);

        await CreateLogoutHandler().HandleAsync(new LogoutCommand("presented"), CancellationToken.None);

        await _dependencies.RefreshTokens.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, default);
    }

    [Fact]
    public async Task ChangePasswordHandleAsync_WrongCurrentPassword_ThrowsAndChangesNothing()
    {
        _dependencies.Users.FindActiveByIdAsync(1, Arg.Any<CancellationToken>()).Returns(AuthTestData.Account());
        _dependencies.PasswordHasher.Verify("wrong", AuthTestData.PasswordHash).Returns(false);

        var act = () => CreateChangePasswordHandler().HandleAsync(
            new ChangePasswordCommand(1, "wrong", NewPassword),
            CancellationToken.None);

        var error = await act.Should().ThrowAsync<DomainException>();
        error.Which.ErrorCode.Should().Be(AuthMessages.CurrentPasswordIncorrectCode);
        await _dependencies.Users.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task ChangePasswordHandleAsync_WeakNewPassword_ThrowsWithPolicyCode()
    {
        _dependencies.Users.FindActiveByIdAsync(1, Arg.Any<CancellationToken>()).Returns(AuthTestData.Account());

        var act = () => CreateChangePasswordHandler().HandleAsync(
            new ChangePasswordCommand(1, AuthTestData.Password, "short"),
            CancellationToken.None);

        var error = await act.Should().ThrowAsync<DomainException>();
        error.Which.ErrorCode.Should().Be(AuthMessages.WeakPasswordCode);
    }

    [Fact]
    public async Task ChangePasswordHandleAsync_ValidChange_SavesNewHashAndRevokesEverySession()
    {
        var account = AuthTestData.Account();
        _dependencies.Users.FindActiveByIdAsync(1, Arg.Any<CancellationToken>()).Returns(account);
        _dependencies.PasswordHasher.Hash(NewPassword).Returns("new-hash");

        await CreateChangePasswordHandler().HandleAsync(
            new ChangePasswordCommand(1, AuthTestData.Password, NewPassword),
            CancellationToken.None);

        account.PasswordHash.Should().Be("new-hash");
        account.PasswordChangedUtc.Should().Be(AuthTestData.Now);
        await _dependencies.RefreshTokens.Received(1).RevokeAllForUserAsync(
            1,
            RefreshToken.RevocationReasons.PasswordChanged,
            AuthTestData.Now,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResetPasswordHandleAsync_UnknownUsername_ThrowsUnknownUser()
    {
        _dependencies.Users.FindActiveByUsernameAsync("nobody", Arg.Any<CancellationToken>())
            .Returns((Domain.Users.UserAccount?)null);

        var act = () => CreateResetPasswordHandler().HandleAsync(
            new ResetPasswordCommand("nobody", NewPassword),
            CancellationToken.None);

        var error = await act.Should().ThrowAsync<DomainException>();
        error.Which.ErrorCode.Should().Be(AuthMessages.UnknownUserCode);
    }

    [Fact]
    public async Task ResetPasswordHandleAsync_ValidReset_SavesWithoutAUserAndRevokesSessions()
    {
        var account = AuthTestData.Account();
        _dependencies.Users.FindActiveByUsernameAsync("dev.doctor", Arg.Any<CancellationToken>()).Returns(account);
        _dependencies.PasswordHasher.Hash(NewPassword).Returns("new-hash");

        await CreateResetPasswordHandler().HandleAsync(
            new ResetPasswordCommand("dev.doctor", NewPassword),
            CancellationToken.None);

        await _dependencies.Users.Received(1).SaveAsync(
            account,
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            null,
            Arg.Any<CancellationToken>());
        await _dependencies.RefreshTokens.Received(1).RevokeAllForUserAsync(
            1,
            RefreshToken.RevocationReasons.PasswordChanged,
            AuthTestData.Now,
            Arg.Any<CancellationToken>());
    }

    private LogoutCommandHandler CreateLogoutHandler() => new(
        new InlineUnitOfWork(),
        _dependencies.RefreshTokens,
        _dependencies.Generator,
        _dependencies.Clock);

    private ChangePasswordCommandHandler CreateChangePasswordHandler() => new(
        new InlineUnitOfWork(),
        _dependencies.Users,
        _dependencies.RefreshTokens,
        _dependencies.PasswordHasher,
        _dependencies.Clock);

    private ResetPasswordCommandHandler CreateResetPasswordHandler() => new(
        new InlineUnitOfWork(),
        _dependencies.Users,
        _dependencies.RefreshTokens,
        _dependencies.PasswordHasher,
        _dependencies.Clock);
}
