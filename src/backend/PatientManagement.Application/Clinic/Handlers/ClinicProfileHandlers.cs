using System.Text.RegularExpressions;
using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Clinic;

namespace PatientManagement.Application.Clinic.Handlers;

/// <summary>Reads the clinic profile.</summary>
public sealed class GetClinicProfileQueryHandler(IClinicProfileRepository clinicProfiles)
{
    public async Task<ClinicProfileDetails> HandleAsync(CancellationToken cancellationToken) =>
        ClinicProfileDetails.From(await clinicProfiles.GetAsync(cancellationToken));
}

/// <summary>Updates the clinic profile. The change and its audit row commit together.</summary>
public sealed class UpdateClinicProfileCommandHandler(
    IUnitOfWork unitOfWork,
    IClinicProfileRepository clinicProfiles)
{
    private static readonly Regex NonDigits = new("[^0-9]", RegexOptions.CultureInvariant);

    public Task<ClinicProfileDetails> HandleAsync(UpdateClinicProfileCommand command, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var profile = await clinicProfiles.GetAsync(token);
            var auditValuesBefore = profile.AuditValues();

            profile.Update(
                Blank(command.ClinicName),
                Blank(command.DoctorName),
                Blank(command.Qualifications),
                Blank(command.RegistrationNumber),
                Blank(command.Address),
                Digits(command.PhoneNumber),
                Blank(command.FooterNotes));

            await clinicProfiles.SaveAsync(profile, auditValuesBefore, command.ChangedByUserId, token);
            return ClinicProfileDetails.From(profile);
        }, cancellationToken);

    // Empty text is stored as NULL, so "not filled in" has one representation.
    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Digits(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : NonDigits.Replace(value, string.Empty);
}
