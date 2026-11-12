using FluentValidation;
using PatientManagement.Application.Appointments;
using PatientManagement.Application.Paging;

namespace PatientManagement.Application.Validators;

/// <summary>Paging limits for a patient's appointment history (CLAUDE.md 6.5).</summary>
public sealed class PatientAppointmentHistoryValidator : AbstractValidator<PatientAppointmentHistoryQuery>
{
    public PatientAppointmentHistoryValidator()
    {
        RuleFor(query => query.PatientId)
            .GreaterThan(0);

        RuleFor(query => query.PageNumber)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, PagingLimits.MaximumPageSize);
    }
}
