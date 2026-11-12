using PatientManagement.Domain.Prescriptions;

namespace PatientManagement.Application.Prescriptions;

/// <summary>Issues the prescription for a completed visit. A visit that already has one returns it unchanged.</summary>
public sealed record GeneratePrescriptionCommand(int VisitId, int ChangedByUserId);

public sealed record GetPrescriptionQuery(int VisitId);

/// <summary>The issued prescription as the API returns it.</summary>
public sealed record PrescriptionDetails(int PrescriptionId, int VisitId, DateOnly PrescriptionDate)
{
    public static PrescriptionDetails From(Prescription prescription) =>
        new(prescription.PrescriptionId, prescription.VisitId, prescription.PrescriptionDate);
}

/// <summary>The clinic header printed on the prescription (the clinic profile).</summary>
public sealed record PrescriptionClinicHeader(
    string? ClinicName,
    string? DoctorName,
    string? Qualifications,
    string? RegistrationNumber,
    string? Address,
    string? PhoneNumber,
    string? FooterNotes);

/// <summary>The patient details printed on the prescription.</summary>
public sealed record PrescriptionPatient(string FullName, DateOnly DateOfBirth, string? PhoneNumber);

/// <summary>The vitals printed on the prescription. Each value is null when it was not measured.</summary>
public sealed record PrescriptionVitals(
    decimal? Temperature,
    int? BloodPressureSystolic,
    int? BloodPressureDiastolic,
    int? Pulse);

public sealed record PrescriptionDiagnosis(string Text, string? Code);

public sealed record PrescriptionMedication(
    string MedicineName,
    string Dosage,
    string Frequency,
    string Duration,
    string? SpecialInstructions);

/// <summary>
/// Everything the printable prescription shows, built from the visit's current records and the clinic profile. Item 41
/// records a snapshot each time it is printed.
/// </summary>
public sealed record PrescriptionDocument(
    PrescriptionDetails Prescription,
    PrescriptionClinicHeader Clinic,
    PrescriptionPatient Patient,
    PrescriptionVitals? Vitals,
    IReadOnlyList<PrescriptionDiagnosis> Diagnoses,
    IReadOnlyList<PrescriptionMedication> Medications);

/// <summary>The issued prescription and whether this request created it (201) or found it already issued (200).</summary>
public sealed record GeneratePrescriptionResult(PrescriptionDetails Prescription, bool Created);
