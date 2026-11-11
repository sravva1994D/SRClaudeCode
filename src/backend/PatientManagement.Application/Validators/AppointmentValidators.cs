using FluentValidation;
using PatientManagement.Application.Appointments;

namespace PatientManagement.Application.Validators;

/// <summary>Format checks for a booking. The slot rules (quarter hour, future only) are domain rules.</summary>
public sealed class ScheduleAppointmentCommandValidator : AbstractValidator<ScheduleAppointmentCommand>
{
    public ScheduleAppointmentCommandValidator()
    {
        RuleFor(command => command.PatientId)
            .GreaterThan(0);

        RuleFor(command => command.SlotStartUtc)
            .NotEqual(default(DateTime))
            .WithMessage("A slot start time is required.");
    }
}
