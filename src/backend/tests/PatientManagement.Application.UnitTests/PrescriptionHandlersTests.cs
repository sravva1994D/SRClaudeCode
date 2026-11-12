using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Prescriptions;
using PatientManagement.Application.Prescriptions.Handlers;
using PatientManagement.Domain.Clinic;
using PatientManagement.Domain.Consultation;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;
using PatientManagement.Domain.Prescriptions;
using PatientManagement.Domain.Visits;

namespace PatientManagement.Application.UnitTests;

public sealed class PrescriptionHandlersTests
{
    private static readonly DateTime Now = new(2026, 11, 12, 10, 30, 0, DateTimeKind.Utc);

    private readonly IVisitRepository _visits = Substitute.For<IVisitRepository>();
    private readonly IMedicationRepository _medications = Substitute.For<IMedicationRepository>();
    private readonly IPrescriptionRepository _prescriptions = Substitute.For<IPrescriptionRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public PrescriptionHandlersTests()
    {
        _clock.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task GenerateHandleAsync_CompletedVisitWithMedications_IssuesAndReportsCreated()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]));
        _medications.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(new List<Medication> { Medication.Reconstitute(1, 3, "Paracetamol", "500mg", "Twice daily", "7 days", null, 0, true, [1]) });
        var handler = NewGenerateHandler();

        var result = await handler.HandleAsync(new GeneratePrescriptionCommand(3, 1), CancellationToken.None);

        result.Created.Should().BeTrue();
        result.Prescription.VisitId.Should().Be(3);
        result.Prescription.PrescriptionDate.Should().Be(new DateOnly(2026, 11, 12));
        await _prescriptions.Received(1).AddAsync(Arg.Any<Prescription>(), 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateHandleAsync_VisitStillInProgress_ThrowsAndIssuesNothing()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]));
        var handler = NewGenerateHandler();

        var act = () => handler.HandleAsync(new GeneratePrescriptionCommand(3, 1), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().Where(error => error.ErrorCode == "visit_not_completed");
        await _prescriptions.DidNotReceive().AddAsync(Arg.Any<Prescription>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateHandleAsync_CompletedVisitWithoutMedications_ThrowsAndIssuesNothing()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]));
        _medications.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>()).Returns(new List<Medication>());
        var handler = NewGenerateHandler();

        var act = () => handler.HandleAsync(new GeneratePrescriptionCommand(3, 1), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().Where(error => error.ErrorCode == "prescription_no_medications");
        await _prescriptions.DidNotReceive().AddAsync(Arg.Any<Prescription>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateHandleAsync_AlreadyIssued_ReturnsTheExistingPrescriptionWithoutWriting()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]));
        _prescriptions.FindActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(Prescription.Reconstitute(9, 3, new DateOnly(2026, 11, 10), true, [1]));
        var handler = NewGenerateHandler();

        var result = await handler.HandleAsync(new GeneratePrescriptionCommand(3, 1), CancellationToken.None);

        result.Created.Should().BeFalse();
        result.Prescription.PrescriptionId.Should().Be(9);
        await _prescriptions.DidNotReceive().AddAsync(Arg.Any<Prescription>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHandleAsync_IssuedPrescription_BuildsTheDocumentFromTheVisitRecords()
    {
        _visits.FindActiveAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]));
        _prescriptions.FindActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(Prescription.Reconstitute(9, 3, new DateOnly(2026, 11, 12), true, [1]));
        var patients = Substitute.For<IPatientRepository>();
        patients.FindActiveAsync(7, Arg.Any<CancellationToken>())
            .Returns(Patient.Reconstitute(7, "Asha Rao", new DateOnly(1990, 5, 14), 2, "9876543210", null, true, [1], null, null));
        var clinic = Substitute.For<IClinicProfileRepository>();
        clinic.GetAsync(Arg.Any<CancellationToken>()).Returns(
            ClinicProfile.Reconstitute("Riverside Clinic", "Dr. Test", "MBBS", "REG-1", null, "9000000000", "Visit again if fever persists", [1]));
        var vitals = Substitute.For<IVitalsRepository>();
        vitals.FindActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(Vitals.Reconstitute(1, 3, null, null, null, 72, true, [1]));
        var diagnoses = Substitute.For<IDiagnosisRepository>();
        diagnoses.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>()).Returns(new List<Diagnosis>());
        _medications.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(new List<Medication> { Medication.Reconstitute(1, 3, "Paracetamol", "500mg", "Twice daily", "7 days", "After food", 0, true, [1]) });
        var handler = new GetPrescriptionQueryHandler(_visits, _prescriptions, patients, clinic, vitals, diagnoses, _medications);

        var document = await handler.HandleAsync(new GetPrescriptionQuery(3), CancellationToken.None);

        document.Clinic.ClinicName.Should().Be("Riverside Clinic");
        document.Patient.FullName.Should().Be("Asha Rao");
        document.Vitals!.Pulse.Should().Be(72);
        document.Medications.Should().ContainSingle(medication => medication.MedicineName == "Paracetamol");
    }

    [Fact]
    public async Task GetHandleAsync_NoPrescriptionIssued_ThrowsNotFound()
    {
        _visits.FindActiveAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]));
        _prescriptions.FindActiveForVisitAsync(3, Arg.Any<CancellationToken>()).Returns((Prescription?)null);
        var handler = new GetPrescriptionQueryHandler(
            _visits,
            _prescriptions,
            Substitute.For<IPatientRepository>(),
            Substitute.For<IClinicProfileRepository>(),
            Substitute.For<IVitalsRepository>(),
            Substitute.For<IDiagnosisRepository>(),
            _medications);

        var act = () => handler.HandleAsync(new GetPrescriptionQuery(3), CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
    }

    private GeneratePrescriptionCommandHandler NewGenerateHandler() =>
        new(new InlineUnitOfWork(), _visits, _medications, _prescriptions, _clock);
}
