using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Auth;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Users;

namespace PatientManagement.Application.Auth.Handlers;

/// <summary>
/// Signs the doctor in. A failed attempt is recorded and committed before the caller sees the generic error, so the
/// lockout count survives it.
/// </summary>
public sealed class LoginCommandHandler(
    IUnitOfWork unitOfWork,
    IUserAccountRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IRefreshTokenGenerator refreshTokenGenerator,
    IAccessTokenIssuer accessTokenIssuer,
    IClock clock,
    IOptions<AuthOptions> options)
{
    public async Task<SessionTokens> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var session = await unitOfWork.ExecuteAsync(
            token => AttemptAsync(command, token),
            cancellationToken);

        return session ?? throw new AuthenticationFailedException(
            AuthMessages.InvalidCredentialsCode,
            AuthMessages.InvalidCredentialsMessage);
    }

    /// <summary>Returns the session, or null when the attempt failed. A failed attempt still commits its counter.</summary>
    private async Task<SessionTokens?> AttemptAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;
        var account = await users.FindActiveByUsernameAsync(command.Username, cancellationToken);

        if (account is null)
        {
            passwordHasher.VerifyAgainstDummy(command.Password);
            return null;
        }

        if (account.IsLockedAt(nowUtc))
        {
            // Same work as a real check, so a locked account is not distinguishable by timing.
            passwordHasher.VerifyAgainstDummy(command.Password);
            return null;
        }

        var auditValuesBefore = account.AuditValues();
        if (!passwordHasher.Verify(command.Password, account.PasswordHash))
        {
            account.RecordFailedLogin(nowUtc);
            await users.SaveAsync(account, auditValuesBefore, changedByUserId: null, cancellationToken);
            return null;
        }

        account.RecordSuccessfulLogin(nowUtc);
        await users.SaveAsync(account, auditValuesBefore, changedByUserId: account.UserId, cancellationToken);

        return await StartSessionAsync(account, nowUtc, cancellationToken);
    }

    private async Task<SessionTokens> StartSessionAsync(UserAccount account, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var plainRefreshToken = refreshTokenGenerator.CreateToken();
        var expiresUtc = nowUtc.AddHours(options.Value.RefreshTokenHours);
        var refreshToken = RefreshToken.Issue(
            account.UserId,
            Guid.NewGuid(),
            refreshTokenGenerator.HashToken(plainRefreshToken),
            nowUtc,
            expiresUtc);
        await refreshTokens.AddAsync(refreshToken, cancellationToken);

        var accessToken = accessTokenIssuer.Issue(account.UserId, account.Username, nowUtc);
        return new SessionTokens(accessToken.Value, accessToken.ExpiresUtc, plainRefreshToken, expiresUtc);
    }
}
