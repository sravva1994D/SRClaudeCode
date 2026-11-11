using FluentValidation;
using PatientManagement.Application.Paging;
using PatientManagement.Application.Patients;

namespace PatientManagement.Application.Validators;

/// <summary>Paging limits and search length. A search term longer than a name or phone number is rejected.</summary>
public sealed class SearchPatientsQueryValidator : AbstractValidator<SearchPatientsQuery>
{
    public const int MaximumSearchTextLength = 100;

    public SearchPatientsQueryValidator()
    {
        RuleFor(query => query.SearchText)
            .MaximumLength(MaximumSearchTextLength)
            .When(query => query.SearchText is not null);

        RuleFor(query => query.PageNumber)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, PagingLimits.MaximumPageSize);
    }
}
