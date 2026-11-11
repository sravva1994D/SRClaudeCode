using FluentValidation;
using PatientManagement.Application.Patients;

namespace PatientManagement.Application.Validators;

/// <summary>Format and length checks for patient details. Date-of-birth-in-future is a domain rule.</summary>
public sealed class PatientFieldsValidator : AbstractValidator<PatientFields>
{
    public const int MaximumNameLength = 200;
    public const int MaximumPhoneLength = 20;
    public const int MaximumAddressLength = 500;
    public const int MaximumChronicConditionsLength = 1000;
    public const int MaximumBloodGroupId = 9;

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

        // The BloodGroups table is seeded with ids 1 to 9 (migration V0002). A value outside that range is rejected here
        // so the database foreign key never has to report it.
        RuleFor(fields => fields.BloodGroupId)
            .InclusiveBetween(1, MaximumBloodGroupId)
            .When(fields => fields.BloodGroupId.HasValue);

        RuleFor(fields => fields.ChronicConditions)
            .MaximumLength(MaximumChronicConditionsLength);
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

/// <summary>The duplicate lookup needs a name and a date of birth. Both use the same limits as the patient form.</summary>
public sealed class FindDuplicatePatientsQueryValidator : AbstractValidator<FindDuplicatePatientsQuery>
{
    public FindDuplicatePatientsQueryValidator()
    {
        RuleFor(query => query.FullName)
            .NotEmpty()
            .MaximumLength(PatientFieldsValidator.MaximumNameLength);

        RuleFor(query => query.DateOfBirth)
            .GreaterThan(new DateOnly(1900, 1, 1));

        RuleFor(query => query.ExcludePatientId)
            .GreaterThan(0)
            .When(query => query.ExcludePatientId.HasValue);
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
