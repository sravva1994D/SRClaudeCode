using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Auth;

namespace PatientManagement.Application.Auth.Handlers;

/// <summary>Ends the session that owns the presented refresh token. Logging out twice is harmless.</summary>
public sealed class LogoutCommandHandler(
    IUnitOfWork unitOfWork,
    IRefreshTokenRepository refreshTokens,
    IRefreshTokenGenerator refreshTokenGenerator,
    IClock clock)
{
    public Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var presented = await refreshTokens.FindByHashAsync(
                refreshTokenGenerator.HashToken(command.RefreshToken),
                token);

            if (presented is null || presented.IsRevoked)
            {
                return;
            }

            var auditValuesBefore = presented.AuditValues();
            presented.Revoke(clock.UtcNow, RefreshToken.RevocationReasons.Logout);
            await refreshTokens.SaveAsync(presented, auditValuesBefore, token);
        }, cancellationToken);
}
