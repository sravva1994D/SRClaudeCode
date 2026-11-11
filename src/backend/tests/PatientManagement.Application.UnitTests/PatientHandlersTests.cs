using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Patients;
using PatientManagement.Application.Patients.Handlers;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;

namespace PatientManagement.Application.UnitTests;

public sealed class PatientHandlersTests
{
    private const int ChangedBy = 1;
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    private readonly IPatientRepository _patients = Substitute.For<IPatientRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public PatientHandlersTests()
    {
        _clock.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task CreateHandleAsync_PhoneWithSymbols_StoresDigitsOnly()
    {
        var command = new CreatePatientCommand(
            new PatientFields("  Asha Rao ", new DateOnly(1990, 5, 14), 2, "+91 (98) 765-43210", "  "),
            ChangedBy);

        await CreateHandler().HandleAsync(command, CancellationToken.None);

        await _patients.Received(1).AddAsync(
            Arg.Is<Patient>(patient =>
                patient.FullName == "Asha Rao"
                && patient.PhoneNumber == "919876543210"
                && patient.Address == null),
            ChangedBy,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHandleAsync_UnknownPatient_ThrowsRecordNotFound()
    {
        _patients.FindActiveAsync(99, Arg.Any<CancellationToken>()).Returns((Patient?)null);

        var act = () => new GetPatientQueryHandler(_patients).HandleAsync(99, CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
    }

    [Fact]
    public async Task UpdateHandleAsync_UnknownPatient_ThrowsRecordNotFoundAndSavesNothing()
    {
        _patients.FindActiveAsync(99, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        var command = new UpdatePatientCommand(99, Fields(), ChangedBy);

        var act = () => UpdateHandler().HandleAsync(command, CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
        await _patients.DidNotReceive().SaveAsync(
            Arg.Any<Patient>(),
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            Arg.Any<AuditAction>(),
            Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteHandleAsync_ActivePatient_DeactivatesAndAuditsDelete()
    {
        var patient = Patient.Reconstitute(7, "Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, true, [1]);
        _patients.FindActiveAsync(7, Arg.Any<CancellationToken>()).Returns(patient);

        await DeleteHandler().HandleAsync(new DeletePatientCommand(7, ChangedBy), CancellationToken.None);

        patient.IsActive.Should().BeFalse();
        await _patients.Received(1).SaveAsync(
            patient,
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            AuditAction.Delete,
            ChangedBy,
            Arg.Any<CancellationToken>());
    }

    private static PatientFields Fields() => new("Asha Rao", new DateOnly(1990, 5, 14), 2, null, null);

    private CreatePatientCommandHandler CreateHandler() =>
        new(new InlineUnitOfWork(), _patients, _clock);

    private UpdatePatientCommandHandler UpdateHandler() =>
        new(new InlineUnitOfWork(), _patients, _clock);

    private DeletePatientCommandHandler DeleteHandler() =>
        new(new InlineUnitOfWork(), _patients);
}
