using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Auth;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Application.Auth.Handlers;

/// <summary>
/// Rotates the refresh token. Presenting a token that was already rotated means it was copied, so the whole family is
/// revoked. Every rejected token is revoked and committed before the caller gets the generic error.
/// </summary>
public sealed class RefreshSessionCommandHandler(
    IUnitOfWork unitOfWork,
    IUserAccountRepository users,
    IRefreshTokenRepository refreshTokens,
    IRefreshTokenGenerator refreshTokenGenerator,
    IAccessTokenIssuer accessTokenIssuer,
    IClock clock,
    IOptions<AuthOptions> options)
{
    public async Task<SessionTokens> HandleAsync(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await unitOfWork.ExecuteAsync(
            token => RotateAsync(command, token),
            cancellationToken);

        return session ?? throw new AuthenticationFailedException(AuthMessages.SessionEndedCode, AuthMessages.SessionEndedMessage);
    }

    private async Task<SessionTokens?> RotateAsync(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        var nowUtc = clock.UtcNow;
        var presented = await refreshTokens.FindByHashAsync(
            refreshTokenGenerator.HashToken(command.RefreshToken),
            cancellationToken);

        if (presented is null)
        {
            return null;
        }

        if (presented.RevokedReason == RefreshToken.RevocationReasons.Rotated)
        {
            await refreshTokens.RevokeFamilyAsync(
                presented.FamilyId,
                RefreshToken.RevocationReasons.ReuseDetected,
                nowUtc,
                cancellationToken);
            return null;
        }

        if (presented.IsRevoked)
        {
            return null;
        }

        var idleTimeout = TimeSpan.FromMinutes(options.Value.IdleTimeoutMinutes);
        if (presented.IsExpiredAt(nowUtc))
        {
            await RevokeAsync(presented, RefreshToken.RevocationReasons.Expired, nowUtc, cancellationToken);
            return null;
        }

        if (presented.IsIdleAt(nowUtc, idleTimeout))
        {
            await RevokeAsync(presented, RefreshToken.RevocationReasons.IdleTimeout, nowUtc, cancellationToken);
            return null;
        }

        var account = await users.FindActiveByIdAsync(presented.UserId, cancellationToken);
        if (account is null)
        {
            await RevokeAsync(presented, RefreshToken.RevocationReasons.AccountInactive, nowUtc, cancellationToken);
            return null;
        }

        await RevokeAsync(presented, RefreshToken.RevocationReasons.Rotated, nowUtc, cancellationToken);

        var plainRefreshToken = refreshTokenGenerator.CreateToken();
        var next = RefreshToken.Continue(presented, refreshTokenGenerator.HashToken(plainRefreshToken), nowUtc);
        await refreshTokens.AddAsync(next, cancellationToken);

        var accessToken = accessTokenIssuer.Issue(account.UserId, account.Username, nowUtc);
        return new SessionTokens(accessToken.Value, accessToken.ExpiresUtc, plainRefreshToken, next.ExpiresUtc);
    }

    private async Task RevokeAsync(RefreshToken token, string reason, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var auditValuesBefore = token.AuditValues();
        token.Revoke(nowUtc, reason);
        await refreshTokens.SaveAsync(token, auditValuesBefore, cancellationToken);
    }
}
