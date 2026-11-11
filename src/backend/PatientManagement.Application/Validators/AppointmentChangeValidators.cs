using FluentValidation;
using PatientManagement.Application.Appointments;

namespace PatientManagement.Application.Validators;

/// <summary>Format checks for a reschedule. The slot rules (quarter hour, future, still scheduled) are domain rules.</summary>
public sealed class RescheduleAppointmentCommandValidator : AbstractValidator<RescheduleAppointmentCommand>
{
    public RescheduleAppointmentCommandValidator()
    {
        RuleFor(command => command.AppointmentId)
            .GreaterThan(0);

        RuleFor(command => command.SlotStartUtc)
            .NotEqual(default(DateTime))
            .WithMessage("A slot start time is required.");
    }
}
