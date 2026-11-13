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

public sealed class PrescriptionPrintHandlersTests
{
    private static readonly DateTime Now = new(2026, 11, 12, 10, 30, 0, DateTimeKind.Utc);

    private readonly IVisitRepository _visits = Substitute.For<IVisitRepository>();
    private readonly IPrescriptionRepository _prescriptions = Substitute.For<IPrescriptionRepository>();
    private readonly IPrescriptionPrintRepository _prints = Substitute.For<IPrescriptionPrintRepository>();
    private readonly IMedicationRepository _medications = Substitute.For<IMedicationRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly FakeCodec _codec = new();
    private GetPrescriptionQueryHandler? _getPrescription;

    public PrescriptionPrintHandlersTests()
    {
        _clock.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task RecordHandleAsync_IssuedPrescription_StoresTheContentAsItIsPrinted()
    {
        SetUpIssuedPrescription();
        var handler = NewRecordHandler();

        var printed = await handler.HandleAsync(new RecordPrescriptionPrintCommand(3, 1), CancellationToken.None);

        printed.PrintedAt.Should().Be(Now);
        printed.Document.Medications.Should().ContainSingle(medication => medication.MedicineName == "Paracetamol");
        await _prints.Received(1).AddAsync(
            Arg.Is<PrescriptionPrint>(print => print.PrescriptionId == 9 && print.SnapshotJson == "snapshot:1"),
            1,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordHandleAsync_NoPrescriptionIssued_ThrowsNotFoundAndStoresNothing()
    {
        _visits.FindActiveAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]));
        _prescriptions.FindActiveForVisitAsync(3, Arg.Any<CancellationToken>()).Returns((Prescription?)null);
        var handler = NewRecordHandler();

        var act = () => handler.HandleAsync(new RecordPrescriptionPrintCommand(3, 1), CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
        await _prints.DidNotReceive().AddAsync(Arg.Any<PrescriptionPrint>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPrintHandleAsync_KnownPrint_ReturnsTheSnapshotNotTheCurrentRecords()
    {
        _visits.FindActiveAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]));
        _prescriptions.FindActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(Prescription.Reconstitute(9, 3, new DateOnly(2026, 11, 12), true, [1]));
        _prints.FindAsync(9, 4, Arg.Any<CancellationToken>())
            .Returns(PrescriptionPrint.Reconstitute(4, 9, Now, "snapshot:1"));
        var handler = new GetPrescriptionPrintQueryHandler(_visits, _prescriptions, _prints, _codec);

        var reprint = await handler.HandleAsync(new GetPrescriptionPrintQuery(3, 4), CancellationToken.None);

        reprint.PrescriptionPrintId.Should().Be(4);
        reprint.PrintedAt.Should().Be(Now);
        reprint.Document.Medications.Should().ContainSingle(medication => medication.MedicineName == "Stored medicine");
    }

    [Fact]
    public async Task GetPrintHandleAsync_PrintOfAnotherPrescription_ThrowsNotFound()
    {
        _visits.FindActiveAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]));
        _prescriptions.FindActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(Prescription.Reconstitute(9, 3, new DateOnly(2026, 11, 12), true, [1]));
        _prints.FindAsync(9, 77, Arg.Any<CancellationToken>()).Returns((PrescriptionPrint?)null);
        var handler = new GetPrescriptionPrintQueryHandler(_visits, _prescriptions, _prints, _codec);

        var act = () => handler.HandleAsync(new GetPrescriptionPrintQuery(3, 77), CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
    }

    private void SetUpIssuedPrescription()
    {
        _visits.FindActiveAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]));
        _prescriptions.FindActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(Prescription.Reconstitute(9, 3, new DateOnly(2026, 11, 12), true, [1]));
        var patients = Substitute.For<IPatientRepository>();
        patients.FindActiveAsync(7, Arg.Any<CancellationToken>())
            .Returns(Patient.Reconstitute(7, "Asha Rao", new DateOnly(1990, 5, 14), 2, "9876543210", null, true, [1], null, null));
        _getPrescription = new GetPrescriptionQueryHandler(
            _visits,
            _prescriptions,
            patients,
            CreateClinic(),
            Substitute.For<IVitalsRepository>(),
            CreateDiagnoses(),
            _medications);
        _medications.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>())
            .Returns(new List<Medication> { Medication.Reconstitute(1, 3, "Paracetamol", "500mg", "Twice daily", "7 days", null, 0, true, [1]) });
    }

    private RecordPrescriptionPrintCommandHandler NewRecordHandler() =>
        new(
            new InlineUnitOfWork(),
            _visits,
            _prescriptions,
            _prints,
            _getPrescription!,
            _codec,
            _clock);

    private static IClinicProfileRepository CreateClinic()
    {
        var clinic = Substitute.For<IClinicProfileRepository>();
        clinic.GetAsync(Arg.Any<CancellationToken>()).Returns(
            ClinicProfile.Reconstitute("Riverside Clinic", "Dr. Test", "MBBS", "REG-1", null, "9000000000", null, [1]));
        return clinic;
    }

    private static IDiagnosisRepository CreateDiagnoses()
    {
        var diagnoses = Substitute.For<IDiagnosisRepository>();
        diagnoses.ListActiveForVisitAsync(3, Arg.Any<CancellationToken>()).Returns(new List<Diagnosis>());
        return diagnoses;
    }

    /// <summary>Stores a snapshot as a short string, so the handlers can be tested without the JSON format.</summary>
    private sealed class FakeCodec : IPrescriptionSnapshotCodec
    {
        public string Serialize(PrescriptionDocument document) => "snapshot:" + document.Medications.Count;

        public PrescriptionDocument Deserialize(string snapshot) => new(
            new PrescriptionDetails(9, 3, new DateOnly(2026, 11, 12)),
            new PrescriptionClinicHeader(null, null, null, null, null, null, null),
            new PrescriptionPatient("Asha Rao", new DateOnly(1990, 5, 14), null),
            null,
            [],
            [new PrescriptionMedication("Stored medicine", "500mg", "Twice daily", "7 days", null)]);
    }
}
