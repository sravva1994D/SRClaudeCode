using FluentValidation;
using PatientManagement.Application.Patients;

namespace PatientManagement.Application.Validators;

/// <summary>Format and length checks for patient details. Date-of-birth-in-future is a domain rule.</summary>
public sealed class PatientFieldsValidator : AbstractValidator<PatientFields>
{
    public const int MaximumNameLength = 200;
    public const int MaximumPhoneLength = 20;
    public const int MaximumAddressLength = 500;

    public PatientFieldsValidator()
    {
        RuleFor(fields => fields.FullName)
            .NotEmpty()
            .MaximumLength(MaximumNameLength);

        RuleFor(fields => fields.DateOfBirth)
            .GreaterThan(new DateOnly(1900, 1, 1));

        RuleFor(fields => fields.GenderId)
            .GreaterThan(0);

        RuleFor(fields => fields.PhoneNumber)
            .MaximumLength(MaximumPhoneLength)
            .Must(phone => phone is null || phone.All(IsPhoneCharacter))
            .WithMessage("Phone number may contain only digits, spaces, +, -, and brackets.");

        RuleFor(fields => fields.Address)
            .MaximumLength(MaximumAddressLength);
    }

    private static bool IsPhoneCharacter(char character) => char.IsDigit(character) || " +-()".Contains(character);
}

public sealed class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(command => command.Fields).SetValidator(new PatientFieldsValidator());
    }
}

public sealed class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientCommandValidator()
    {
        RuleFor(command => command.PatientId).GreaterThan(0);
        RuleFor(command => command.Fields).SetValidator(new PatientFieldsValidator());
    }
}
