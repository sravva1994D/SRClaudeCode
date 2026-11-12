using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Visits;
using PatientManagement.Application.Visits.Handlers;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Consultation;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Visits;

namespace PatientManagement.Application.UnitTests;

public sealed class CompleteVisitHandlerTests
{
    private readonly IVisitRepository _visits = Substitute.For<IVisitRepository>();
    private readonly IVitalsRepository _vitals = Substitute.For<IVitalsRepository>();
    private readonly IAppointmentRepository _appointments = Substitute.For<IAppointmentRepository>();

    [Fact]
    public async Task HandleAsync_VisitWithVitals_CompletesTheVisitAndTheAppointment()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]));
        _vitals.FindActiveForVisitAsync(3, Arg.Any<CancellationToken>()).Returns(Vitals.Reconstitute(1, 3, null, null, null, 72, true, [1]));
        _appointments.FindActiveAsync(5, Arg.Any<CancellationToken>())
            .Returns(Appointment.Reconstitute(5, 7, new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc), AppointmentStatuses.Scheduled, true, [1]));
        var handler = new CompleteVisitCommandHandler(new InlineUnitOfWork(), _visits, _vitals, _appointments);

        var details = await handler.HandleAsync(new CompleteVisitCommand(3, 1), CancellationToken.None);

        details.StatusId.Should().Be(VisitStatuses.Completed);
        await _visits.Received(1).SaveStatusAsync(
            Arg.Is<Visit>(visit => visit.StatusId == VisitStatuses.Completed), Arg.Any<IReadOnlyDictionary<string, object?>>(), 1, Arg.Any<CancellationToken>());
        await _appointments.Received(1).SaveAsync(
            Arg.Is<Appointment>(appointment => appointment.StatusId == AppointmentStatuses.Completed),
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            AuditAction.Update,
            1,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_NoVitals_ThrowsVitalsRequiredAndWritesNothing()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.InProgress, true, [1]));
        _vitals.FindActiveForVisitAsync(3, Arg.Any<CancellationToken>()).Returns((Vitals?)null);
        var handler = new CompleteVisitCommandHandler(new InlineUnitOfWork(), _visits, _vitals, _appointments);

        var act = () => handler.HandleAsync(new CompleteVisitCommand(3, 1), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().Where(error => error.ErrorCode == "vitals_required");
        await _visits.DidNotReceive().SaveStatusAsync(Arg.Any<Visit>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _appointments.DidNotReceive().SaveAsync(Arg.Any<Appointment>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<AuditAction>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_CompletedVisit_ThrowsNotInProgress()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, 5, VisitStatuses.Completed, true, [1]));
        var handler = new CompleteVisitCommandHandler(new InlineUnitOfWork(), _visits, _vitals, _appointments);

        var act = () => handler.HandleAsync(new CompleteVisitCommand(3, 1), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().Where(error => error.ErrorCode == "visit_not_in_progress");
        await _visits.DidNotReceive().SaveStatusAsync(Arg.Any<Visit>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_VisitWithoutAppointment_CompletesOnlyTheVisit()
    {
        _visits.FindActiveForUpdateAsync(3, Arg.Any<CancellationToken>())
            .Returns(Visit.Reconstitute(3, 7, null, VisitStatuses.InProgress, true, [1]));
        _vitals.FindActiveForVisitAsync(3, Arg.Any<CancellationToken>()).Returns(Vitals.Reconstitute(1, 3, null, null, null, 72, true, [1]));
        var handler = new CompleteVisitCommandHandler(new InlineUnitOfWork(), _visits, _vitals, _appointments);

        await handler.HandleAsync(new CompleteVisitCommand(3, 1), CancellationToken.None);

        await _appointments.DidNotReceive().FindActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UnknownVisit_ThrowsNotFound()
    {
        _visits.FindActiveForUpdateAsync(99, Arg.Any<CancellationToken>()).Returns((Visit?)null);
        var handler = new CompleteVisitCommandHandler(new InlineUnitOfWork(), _visits, _vitals, _appointments);

        var act = () => handler.HandleAsync(new CompleteVisitCommand(99, 1), CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
    }
}
