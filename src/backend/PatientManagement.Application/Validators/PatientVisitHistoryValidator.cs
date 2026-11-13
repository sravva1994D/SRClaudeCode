using FluentValidation;
using PatientManagement.Application.Paging;
using PatientManagement.Application.Visits;

namespace PatientManagement.Application.Validators;

/// <summary>Paging and date-range rules for a patient's visit history (CLAUDE.md 6.5, 6.6).</summary>
public sealed class PatientVisitHistoryValidator : AbstractValidator<PatientVisitHistoryQuery>
{
    public PatientVisitHistoryValidator()
    {
        RuleFor(query => query.PatientId)
            .GreaterThan(0);

        RuleFor(query => query.PageNumber)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, PagingLimits.MaximumPageSize);

        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From)
            .When(query => query.From.HasValue && query.To.HasValue)
            .WithMessage("The end date must be on or after the start date.");
    }
}
