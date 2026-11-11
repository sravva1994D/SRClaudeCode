namespace PatientManagement.Application.Patients;

/// <summary>
/// Searches active patients. <paramref name="SearchText"/> is matched against the name when it contains letters and
/// against the phone number when it contains only digits and phone symbols. Null or blank returns everyone.
/// </summary>
public sealed record SearchPatientsQuery(string? SearchText, int PageNumber, int PageSize);

/// <summary>A row in the patient list. Contact details are included for the list; clinical data is not.</summary>
public sealed record PatientListItem(
    int PatientId,
    string FullName,
    DateOnly DateOfBirth,
    int GenderId,
    string? PhoneNumber);
