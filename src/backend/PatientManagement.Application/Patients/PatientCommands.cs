using PatientManagement.Domain.Patients;

namespace PatientManagement.Application.Patients;

/// <summary>The editable patient details shared by create and update.</summary>
public sealed record PatientFields(
    string FullName,
    DateOnly DateOfBirth,
    int GenderId,
    string? PhoneNumber,
    string? Address,
    int? BloodGroupId = null,
    string? ChronicConditions = null);

public sealed record CreatePatientCommand(PatientFields Fields, int ChangedByUserId);

public sealed record UpdatePatientCommand(int PatientId, PatientFields Fields, int ChangedByUserId);

public sealed record DeletePatientCommand(int PatientId, int ChangedByUserId);

/// <summary>What the API returns for a patient.</summary>
public sealed record PatientDetails(
    int PatientId,
    string FullName,
    DateOnly DateOfBirth,
    int GenderId,
    string? PhoneNumber,
    string? Address,
    int? BloodGroupId,
    string? ChronicConditions)
{
    public static PatientDetails From(Patient patient) => new(
        patient.PatientId,
        patient.FullName,
        patient.DateOfBirth,
        patient.GenderId,
        patient.PhoneNumber,
        patient.Address,
        patient.BloodGroupId,
        patient.ChronicConditions);
}
