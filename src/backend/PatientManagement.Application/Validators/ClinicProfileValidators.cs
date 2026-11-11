using FluentValidation;
using PatientManagement.Application.Clinic;

namespace PatientManagement.Application.Validators;

/// <summary>Length limits match the ClinicProfile columns (migration V0003). Phone numbers may use spaces and symbols.</summary>
public sealed class UpdateClinicProfileCommandValidator : AbstractValidator<UpdateClinicProfileCommand>
{
    public const int MaximumNameLength = 200;
    public const int MaximumRegistrationNumberLength = 50;
    public const int MaximumAddressLength = 500;
    public const int MaximumPhoneLength = 20;
    public const int MaximumFooterLength = 1000;

    public UpdateClinicProfileCommandValidator()
    {
        RuleFor(command => command.ClinicName).MaximumLength(MaximumNameLength);
        RuleFor(command => command.DoctorName).MaximumLength(MaximumNameLength);
        RuleFor(command => command.Qualifications).MaximumLength(MaximumNameLength);
        RuleFor(command => command.RegistrationNumber).MaximumLength(MaximumRegistrationNumberLength);
        RuleFor(command => command.Address).MaximumLength(MaximumAddressLength);
        RuleFor(command => command.PhoneNumber).MaximumLength(MaximumPhoneLength);
        RuleFor(command => command.FooterNotes).MaximumLength(MaximumFooterLength);

        RuleFor(command => command.PhoneNumber)
            .Must(phone => phone is null || phone.All(character => char.IsDigit(character) || " +-()".Contains(character)))
            .WithMessage("Phone number may contain only digits, spaces, +, -, and brackets.");
    }
}
