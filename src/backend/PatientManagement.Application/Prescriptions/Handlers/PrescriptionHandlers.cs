using PatientManagement.Application.Interfaces;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Prescriptions;

namespace PatientManagement.Application.Prescriptions.Handlers;

/// <summary>
/// Issues the prescription for a completed visit that has at least one medication. The visit row is locked, so two requests
/// for one visit cannot both issue a prescription; the second finds the first and returns it.
/// </summary>
public sealed class GeneratePrescriptionCommandHandler(
    IUnitOfWork unitOfWork,
    IVisitRepository visits,
    IMedicationRepository medications,
    IPrescriptionRepository prescriptions,
    IClock clock)
{
    public Task<GeneratePrescriptionResult> HandleAsync(
        GeneratePrescriptionCommand command,
        CancellationToken cancellationToken) =>
        unitOfWork.ExecuteAsync(async token =>
        {
            var visit = await visits.FindActiveForUpdateAsync(command.VisitId, token)
                ?? throw new RecordNotFoundException("Visit");
            visit.EnsureCompleted();

            var existing = await prescriptions.FindActiveForVisitAsync(visit.VisitId, token);
            if (existing is not null)
            {
                return new GeneratePrescriptionResult(PrescriptionDetails.From(existing), Created: false);
            }

            var saved = await medications.ListActiveForVisitAsync(visit.VisitId, token);
            if (saved.Count == 0)
            {
                throw new DomainException(
                    "prescription_no_medications",
                    "Add at least one medication before generating a prescription.");
            }

            var prescription = Prescription.Issue(visit.VisitId, DateOnly.FromDateTime(clock.UtcNow));
            await prescriptions.AddAsync(prescription, command.ChangedByUserId, token);
            return new GeneratePrescriptionResult(PrescriptionDetails.From(prescription), Created: true);
        }, cancellationToken);
}

/// <summary>
/// Builds the printable prescription from the visit's records and the clinic profile. Read-only: no transaction. Vitals are
/// optional, so a visit without them still prints.
/// </summary>
public sealed class GetPrescriptionQueryHandler(
    IVisitRepository visits,
    IPrescriptionRepository prescriptions,
    IPatientRepository patients,
    IClinicProfileRepository clinicProfile,
    IVitalsRepository vitals,
    IDiagnosisRepository diagnoses,
    IMedicationRepository medications)
{
    public async Task<PrescriptionDocument> HandleAsync(GetPrescriptionQuery query, CancellationToken cancellationToken)
    {
        var visit = await visits.FindActiveAsync(query.VisitId, cancellationToken)
            ?? throw new RecordNotFoundException("Visit");
        var prescription = await prescriptions.FindActiveForVisitAsync(visit.VisitId, cancellationToken)
            ?? throw new RecordNotFoundException("Prescription");
        var patient = await patients.FindActiveAsync(visit.PatientId, cancellationToken)
            ?? throw new RecordNotFoundException("Patient");
        var profile = await clinicProfile.GetAsync(cancellationToken);
        var recordedVitals = await vitals.FindActiveForVisitAsync(visit.VisitId, cancellationToken);
        var savedDiagnoses = await diagnoses.ListActiveForVisitAsync(visit.VisitId, cancellationToken);
        var savedMedications = await medications.ListActiveForVisitAsync(visit.VisitId, cancellationToken);

        return new PrescriptionDocument(
            PrescriptionDetails.From(prescription),
            new PrescriptionClinicHeader(
                profile.ClinicName,
                profile.DoctorName,
                profile.Qualifications,
                profile.RegistrationNumber,
                profile.Address,
                profile.PhoneNumber,
                profile.FooterNotes),
            new PrescriptionPatient(patient.FullName, patient.DateOfBirth, patient.PhoneNumber),
            recordedVitals is null
                ? null
                : new PrescriptionVitals(
                    recordedVitals.Temperature,
                    recordedVitals.BloodPressureSystolic,
                    recordedVitals.BloodPressureDiastolic,
                    recordedVitals.Pulse),
            savedDiagnoses.Select(diagnosis => new PrescriptionDiagnosis(diagnosis.Text, diagnosis.Code)).ToList(),
            savedMedications.Select(medication => new PrescriptionMedication(
                medication.MedicineName,
                medication.Dosage,
                medication.Frequency,
                medication.Duration,
                medication.SpecialInstructions)).ToList());
    }
}
