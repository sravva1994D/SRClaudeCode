using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Domain.Patients;

/// <summary>A patient registered with the clinic. Patients are deactivated, never deleted (CLAUDE.md 6.5).</summary>
public sealed class Patient
{
    private Patient(
        int patientId,
        string fullName,
        DateOnly dateOfBirth,
        int genderId,
        string? phoneNumber,
        string? address,
        bool isActive,
        byte[] rowVersion)
    {
        PatientId = patientId;
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        GenderId = genderId;
        PhoneNumber = phoneNumber;
        Address = address;
        IsActive = isActive;
        RowVersion = rowVersion;
    }

    public int PatientId { get; private set; }

    public string FullName { get; private set; }

    public DateOnly DateOfBirth { get; private set; }

    public int GenderId { get; private set; }

    /// <summary>Digits only (CLAUDE.md 6.6).</summary>
    public string? PhoneNumber { get; private set; }

    public string? Address { get; private set; }

    public bool IsActive { get; private set; }

    public byte[] RowVersion { get; private set; }

    /// <summary>Creates a patient that has not been saved yet. The repository assigns the id on insert.</summary>
    public static Patient Register(
        string fullName,
        DateOnly dateOfBirth,
        int genderId,
        string? phoneNumber,
        string? address,
        DateOnly today)
    {
        EnsureDateOfBirthIsNotInFuture(dateOfBirth, today);
        return new Patient(0, fullName, dateOfBirth, genderId, phoneNumber, address, isActive: true, Array.Empty<byte>());
    }

    /// <summary>Rebuilds a patient from a stored row.</summary>
    public static Patient Reconstitute(
        int patientId,
        string fullName,
        DateOnly dateOfBirth,
        int genderId,
        string? phoneNumber,
        string? address,
        bool isActive,
        byte[] rowVersion) =>
        new(patientId, fullName, dateOfBirth, genderId, phoneNumber, address, isActive, rowVersion);

    public void Update(
        string fullName,
        DateOnly dateOfBirth,
        int genderId,
        string? phoneNumber,
        string? address,
        DateOnly today)
    {
        EnsureDateOfBirthIsNotInFuture(dateOfBirth, today);
        FullName = fullName;
        DateOfBirth = dateOfBirth;
        GenderId = genderId;
        PhoneNumber = phoneNumber;
        Address = address;
    }

    /// <summary>Hides the patient from every normal read. The row and its history stay in the database.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Called by the repository after insert, when the database has assigned the id.</summary>
    public void AssignPatientId(int patientId) => PatientId = patientId;

    /// <summary>Called by the repository after each save, with the new row version.</summary>
    public void SetRowVersion(byte[] rowVersion) => RowVersion = rowVersion;

    /// <summary>Values written to the audit row. Only the audit table stores them, never the logs.</summary>
    public IReadOnlyDictionary<string, object?> AuditValues() => new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["FullName"] = FullName,
        ["DateOfBirth"] = DateOfBirth.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        ["GenderId"] = GenderId,
        ["PhoneNumber"] = PhoneNumber,
        ["Address"] = Address,
        ["IsActive"] = IsActive,
    };

    private static void EnsureDateOfBirthIsNotInFuture(DateOnly dateOfBirth, DateOnly today)
    {
        if (dateOfBirth > today)
        {
            throw new DomainException("date_of_birth_in_future", "Date of birth cannot be in the future.");
        }
    }
}
