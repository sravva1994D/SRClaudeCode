using FluentAssertions;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Validators;

namespace PatientManagement.Application.UnitTests;

public sealed class ChangePasswordCommandValidatorTests
{
    private const string CurrentPassword = "Passw0rd!";

    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public void Validate_PolicySatisfyingPassword_HasNoErrors()
    {
        var result = _validator.Validate(new ChangePasswordCommand(1, CurrentPassword, "NewPassw0rd!"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WeakNewPassword_ReportsNewPasswordError()
    {
        var result = _validator.Validate(new ChangePasswordCommand(1, CurrentPassword, "short"));

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact]
    public void Validate_EmptyNewPassword_ReportsNewPasswordError()
    {
        var result = _validator.Validate(new ChangePasswordCommand(1, CurrentPassword, string.Empty));

        result.Errors.Should().Contain(error => error.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }
}
