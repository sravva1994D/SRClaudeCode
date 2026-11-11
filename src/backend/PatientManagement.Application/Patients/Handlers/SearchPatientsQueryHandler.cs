using System.Text.RegularExpressions;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Paging;

namespace PatientManagement.Application.Patients.Handlers;

/// <summary>Returns one page of active patients, filtered by name or phone number.</summary>
public sealed class SearchPatientsQueryHandler(IPatientSearch patientSearch)
{
    private static readonly Regex Letters = new("\\p{L}", RegexOptions.CultureInvariant);
    private static readonly Regex PhoneSymbols = new("^[0-9 +()-]+$", RegexOptions.CultureInvariant);
    private static readonly Regex NonDigits = new("[^0-9]", RegexOptions.CultureInvariant);

    public Task<PagedResponse<PatientListItem>> HandleAsync(SearchPatientsQuery query, CancellationToken cancellationToken)
    {
        var (nameTerm, phoneDigits) = Classify(query.SearchText);
        return patientSearch.SearchActiveAsync(
            nameTerm,
            phoneDigits,
            query.PageNumber,
            query.PageSize,
            cancellationToken);
    }

    /// <summary>
    /// A term with letters searches names only. A term with only digits and phone symbols searches phone numbers
    /// only, using its digits. Anything else searches nothing and returns the full list.
    /// </summary>
    public static (string? NameTerm, string? PhoneDigits) Classify(string? searchText)
    {
        var term = searchText?.Trim();
        if (string.IsNullOrEmpty(term))
        {
            return (null, null);
        }
        if (Letters.IsMatch(term))
        {
            return (term, null);
        }
        if (PhoneSymbols.IsMatch(term))
        {
            var digits = NonDigits.Replace(term, string.Empty);
            return digits.Length > 0 ? (null, digits) : (null, null);
        }
        return (null, null);
    }
}
