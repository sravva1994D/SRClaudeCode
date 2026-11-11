namespace PatientManagement.Domain.Clinic;

/// <summary>The prescription header and footer settings (Q4). One row, edited by the doctor.</summary>
public sealed class ClinicProfile
{
    private ClinicProfile(
        string? clinicName,
        string? doctorName,
        string? qualifications,
        string? registrationNumber,
        string? address,
        string? phoneNumber,
        string? footerNotes,
        byte[] rowVersion)
    {
        ClinicName = clinicName;
        DoctorName = doctorName;
        Qualifications = qualifications;
        RegistrationNumber = registrationNumber;
        Address = address;
        PhoneNumber = phoneNumber;
        FooterNotes = footerNotes;
        RowVersion = rowVersion;
    }

    public string? ClinicName { get; private set; }

    public string? DoctorName { get; private set; }

    public string? Qualifications { get; private set; }

    public string? RegistrationNumber { get; private set; }

    public string? Address { get; private set; }

    /// <summary>Digits only (CLAUDE.md 6.6). The client applies the display format.</summary>
    public string? PhoneNumber { get; private set; }

    public string? FooterNotes { get; private set; }

    public byte[] RowVersion { get; private set; }

    public static ClinicProfile Reconstitute(
        string? clinicName,
        string? doctorName,
        string? qualifications,
        string? registrationNumber,
        string? address,
        string? phoneNumber,
        string? footerNotes,
        byte[] rowVersion) =>
        new(clinicName, doctorName, qualifications, registrationNumber, address, phoneNumber, footerNotes, rowVersion);

    public void Update(
        string? clinicName,
        string? doctorName,
        string? qualifications,
        string? registrationNumber,
        string? address,
        string? phoneNumber,
        string? footerNotes)
    {
        ClinicName = clinicName;
        DoctorName = doctorName;
        Qualifications = qualifications;
        RegistrationNumber = registrationNumber;
        Address = address;
        PhoneNumber = phoneNumber;
        FooterNotes = footerNotes;
    }

    public void SetRowVersion(byte[] rowVersion) => RowVersion = rowVersion;

    public IReadOnlyDictionary<string, object?> AuditValues() => new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["ClinicName"] = ClinicName,
        ["DoctorName"] = DoctorName,
        ["Qualifications"] = Qualifications,
        ["RegistrationNumber"] = RegistrationNumber,
        ["Address"] = Address,
        ["PhoneNumber"] = PhoneNumber,
        ["FooterNotes"] = FooterNotes,
    };
}
