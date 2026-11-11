using FluentValidation;
using PatientManagement.Application.Auth;
using PatientManagement.Domain.Security;

namespace PatientManagement.Application.Validators;

/// <summary>Format and length checks for login (CLAUDE.md 6.6). Credential checks happen in the handler.</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public const int MaximumUsernameLength = 64;
    public const int MaximumPasswordLength = 256;

    public LoginCommandValidator()
    {
        RuleFor(command => command.Username)
            .NotEmpty()
            .MaximumLength(MaximumUsernameLength);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(MaximumPasswordLength);
    }
}

/// <summary>
/// Format, length and policy checks for changing the password. The handler repeats the policy check as a safety net.
/// A weak new password is a request error (400), not a business-rule error, so it is checked here.
/// </summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public const string WeakNewPasswordMessage =
        "The new password must be at least 8 characters and include upper case, lower case, a digit and a symbol.";

    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword)
            .NotEmpty()
            .MaximumLength(LoginCommandValidator.MaximumPasswordLength);

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .MaximumLength(LoginCommandValidator.MaximumPasswordLength)
            .Must(password => string.IsNullOrEmpty(password) || PasswordPolicy.IsSatisfiedBy(password))
            .WithMessage(WeakNewPasswordMessage);
    }
}
