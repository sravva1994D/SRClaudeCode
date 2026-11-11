using FluentValidation;
using PatientManagement.Application.Auth;

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

/// <summary>Format and length checks for changing the password. The policy itself is checked in the handler.</summary>
public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(command => command.CurrentPassword)
            .NotEmpty()
            .MaximumLength(LoginCommandValidator.MaximumPasswordLength);

        RuleFor(command => command.NewPassword)
            .NotEmpty()
            .MaximumLength(LoginCommandValidator.MaximumPasswordLength);
    }
}
