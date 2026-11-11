using FluentValidation;
using PatientManagement.Application.Appointments;

namespace PatientManagement.Application.Validators;

/// <summary>The range must be ordered and no longer than a month, so one request cannot read the whole table.</summary>
public sealed class ListAppointmentsQueryValidator : AbstractValidator<ListAppointmentsQuery>
{
    public const int MaximumRangeDays = 31;

    public ListAppointmentsQueryValidator()
    {
        RuleFor(query => query.FromUtc)
            .NotEqual(default(DateTime))
            .WithMessage("A start time is required.");

        RuleFor(query => query.ToUtc)
            .NotEqual(default(DateTime))
            .WithMessage("An end time is required.");

        RuleFor(query => query)
            .Must(query => query.ToUtc > query.FromUtc)
            .WithMessage("The end time must be after the start time.")
            .Must(query => query.ToUtc - query.FromUtc <= TimeSpan.FromDays(MaximumRangeDays))
            .WithMessage($"The range can be at most {MaximumRangeDays} days.")
            .When(query => query.FromUtc != default && query.ToUtc != default);
    }
}
