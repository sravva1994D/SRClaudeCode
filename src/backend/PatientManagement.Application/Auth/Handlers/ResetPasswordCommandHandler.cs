using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Auth;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Security;

namespace PatientManagement.Application.Auth.Handlers;

/// <summary>
/// Operator reset (plan step 17, CLAUDE.md 14). Run from the console by someone with database access, not from the
/// web app. The change is audited with no user, because the operator is not a signed-in account.
/// </summary>
public sealed class ResetPasswordCommandHandler(
    IUnitOfWork unitOfWork,
    IUserAccountRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IClock clock)
{
    public Task HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var account = await users.FindActiveByUsernameAsync(command.Username, token)
                ?? throw new DomainException(AuthMessages.UnknownUserCode, AuthMessages.UnknownUserMessage);

            if (!PasswordPolicy.IsSatisfiedBy(command.NewPassword))
            {
                throw new DomainException(AuthMessages.WeakPasswordCode, AuthMessages.WeakPasswordMessage);
            }

            var nowUtc = clock.UtcNow;
            var auditValuesBefore = account.AuditValues();
            account.ChangePassword(passwordHasher.Hash(command.NewPassword), nowUtc);
            await users.SaveAsync(account, auditValuesBefore, changedByUserId: null, token);

            await refreshTokens.RevokeAllForUserAsync(
                account.UserId,
                RefreshToken.RevocationReasons.PasswordChanged,
                nowUtc,
                token);
        }, cancellationToken);
}
