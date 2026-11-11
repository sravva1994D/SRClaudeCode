using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Patients;
using PatientManagement.Application.Patients.Handlers;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;

namespace PatientManagement.Application.UnitTests;

public sealed class RecentPatientsTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    private readonly IPatientRepository _patients = Substitute.For<IPatientRepository>();
    private readonly IRecentPatientRepository _recents = Substitute.For<IRecentPatientRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public RecentPatientsTests()
    {
        _clock.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task RecordViewHandleAsync_ActivePatient_RecordsTheViewWithTheKeepLimit()
    {
        var patient = Patient.Reconstitute(7, "Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, true, [1]);
        _patients.FindActiveAsync(7, Arg.Any<CancellationToken>()).Returns(patient);
        var handler = new RecordPatientViewedCommandHandler(new InlineUnitOfWork(), _patients, _recents, _clock);

        await handler.HandleAsync(new RecordPatientViewedCommand(7, 1), CancellationToken.None);

        await _recents.Received(1).RecordViewAsync(1, 7, Now, RecentPatientsPolicy.KeepCount, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordViewHandleAsync_UnknownPatient_ThrowsAndRecordsNothing()
    {
        _patients.FindActiveAsync(99, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        var handler = new RecordPatientViewedCommandHandler(new InlineUnitOfWork(), _patients, _recents, _clock);

        var act = () => handler.HandleAsync(new RecordPatientViewedCommand(99, 1), CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
        await _recents.DidNotReceive().RecordViewAsync(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void KeepCount_IsTwentyPatients()
    {
        RecentPatientsPolicy.KeepCount.Should().Be(20);
    }
}
