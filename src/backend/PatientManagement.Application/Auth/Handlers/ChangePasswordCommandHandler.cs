using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Auth;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Security;

namespace PatientManagement.Application.Auth.Handlers;

/// <summary>Changes the signed-in doctor's password and ends every session (CLAUDE.md 10.1).</summary>
public sealed class ChangePasswordCommandHandler(
    IUnitOfWork unitOfWork,
    IUserAccountRepository users,
    IRefreshTokenRepository refreshTokens,
    IPasswordHasher passwordHasher,
    IClock clock)
{
    public Task HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var account = await users.FindActiveByIdAsync(command.UserId, token)
                ?? throw new AuthenticationFailedException(AuthMessages.SessionEndedCode, AuthMessages.SessionEndedMessage);

            if (!passwordHasher.Verify(command.CurrentPassword, account.PasswordHash))
            {
                throw new DomainException(AuthMessages.CurrentPasswordIncorrectCode, AuthMessages.CurrentPasswordIncorrectMessage);
            }

            if (!PasswordPolicy.IsSatisfiedBy(command.NewPassword))
            {
                throw new DomainException(AuthMessages.WeakPasswordCode, AuthMessages.WeakPasswordMessage);
            }

            var nowUtc = clock.UtcNow;
            var auditValuesBefore = account.AuditValues();
            account.ChangePassword(passwordHasher.Hash(command.NewPassword), nowUtc);
            await users.SaveAsync(account, auditValuesBefore, changedByUserId: account.UserId, token);

            await refreshTokens.RevokeAllForUserAsync(
                account.UserId,
                RefreshToken.RevocationReasons.PasswordChanged,
                nowUtc,
                token);
        }, cancellationToken);
}
