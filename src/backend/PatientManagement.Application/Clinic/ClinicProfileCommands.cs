using PatientManagement.Domain.Clinic;

namespace PatientManagement.Application.Clinic;

/// <summary>What the clinic profile screen shows and what the prescription header prints.</summary>
public sealed record ClinicProfileDetails(
    string? ClinicName,
    string? DoctorName,
    string? Qualifications,
    string? RegistrationNumber,
    string? Address,
    string? PhoneNumber,
    string? FooterNotes)
{
    public static ClinicProfileDetails From(ClinicProfile profile) => new(
        profile.ClinicName,
        profile.DoctorName,
        profile.Qualifications,
        profile.RegistrationNumber,
        profile.Address,
        profile.PhoneNumber,
        profile.FooterNotes);
}

public sealed record UpdateClinicProfileCommand(
    string? ClinicName,
    string? DoctorName,
    string? Qualifications,
    string? RegistrationNumber,
    string? Address,
    string? PhoneNumber,
    string? FooterNotes,
    int ChangedByUserId);
