using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Consultation;
using PatientManagement.Application.Consultation.Handlers;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Visits;
using PatientManagement.Domain.Consultation;

namespace PatientManagement.Application.UnitTests;

public sealed class VitalsHandlersTests
{
    private readonly IVisitRepository _visits = Substitute.For<IVisitRepository>();
    private readonly IVitalRangeRepository _ranges = Substitute.For<IVitalRangeRepository>();
    private readonly IVitalsRepository _vitals = Substitute.For<IVitalsRepository>();

    [Fact]
    public async Task RecordHandleAsync_InProgressVisit_SavesAndReturnsTheAbnormalWarning()
    {
        _visits.FindActiveAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]));
        _ranges.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(
            new Dictionary<string, VitalRange> { [VitalSigns.Pulse] = new(VitalSigns.Pulse, 60, 100, 20, 250) });
        var handler = new RecordVitalsCommandHandler(new InlineUnitOfWork(), _visits, _ranges, _vitals);

        var result = await handler.HandleAsync(
            new RecordVitalsCommand(3, null, null, null, 120, 1),
            CancellationToken.None);

        result.Warnings.Should().Equal(VitalSigns.Pulse);
        result.Vitals.Pulse.Should().Be(120);
        await _vitals.Received(1).AddAsync(Arg.Any<Vitals>(), 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordHandleAsync_CompletedVisit_ThrowsAndSavesNothing()
    {
        _visits.FindActiveAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]));
        var handler = new RecordVitalsCommandHandler(new InlineUnitOfWork(), _visits, _ranges, _vitals);

        var act = () => handler.HandleAsync(new RecordVitalsCommand(3, null, null, null, 72, 1), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _vitals.DidNotReceive().AddAsync(Arg.Any<Vitals>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RecordHandleAsync_UnknownVisit_ThrowsNotFound()
    {
        _visits.FindActiveAsync(99, Arg.Any<CancellationToken>()).Returns((Visit?)null);
        var handler = new RecordVitalsCommandHandler(new InlineUnitOfWork(), _visits, _ranges, _vitals);

        var act = () => handler.HandleAsync(new RecordVitalsCommand(99, null, null, null, 72, 1), CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
    }
}
