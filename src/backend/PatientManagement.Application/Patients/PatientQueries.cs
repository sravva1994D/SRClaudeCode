using PatientManagement.Domain.Patients;

namespace PatientManagement.Application.Patients;

/// <summary>Looks for active patients with the same name and date of birth, to warn before a duplicate is registered.</summary>
public sealed record FindDuplicatePatientsQuery(string FullName, DateOnly DateOfBirth, int? ExcludePatientId);

/// <summary>The few fields a duplicate warning shows. It never includes contact details or clinical data.</summary>
public sealed record PatientSummary(int PatientId, string FullName, DateOnly DateOfBirth)
{
    public static PatientSummary From(Patient patient) => new(patient.PatientId, patient.FullName, patient.DateOfBirth);
}
