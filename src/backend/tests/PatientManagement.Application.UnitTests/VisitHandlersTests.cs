using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Visits;
using PatientManagement.Application.Visits.Handlers;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Visits;

namespace PatientManagement.Application.UnitTests;

public sealed class VisitHandlersTests
{
    private static readonly DateTime SlotStart = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly IAppointmentRepository _appointments = Substitute.For<IAppointmentRepository>();
    private readonly IVisitRepository _visits = Substitute.For<IVisitRepository>();

    [Fact]
    public async Task StartVisitHandleAsync_ScheduledAppointment_AddsAnInProgressVisit()
    {
        _appointments.FindActiveAsync(5, Arg.Any<CancellationToken>())
            .Returns(Appointment.Reconstitute(5, 7, SlotStart, AppointmentStatuses.Scheduled, true, [1]));
        var handler = new StartVisitCommandHandler(new InlineUnitOfWork(), _appointments, _visits);

        var started = await handler.HandleAsync(new StartVisitCommand(5, 1), CancellationToken.None);

        started.StatusId.Should().Be(VisitStatuses.InProgress);
        started.AppointmentId.Should().Be(5);
        await _visits.Received(1).AddAsync(Arg.Any<Visit>(), 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartVisitHandleAsync_CancelledAppointment_ThrowsAndAddsNothing()
    {
        _appointments.FindActiveAsync(5, Arg.Any<CancellationToken>())
            .Returns(Appointment.Reconstitute(5, 7, SlotStart, AppointmentStatuses.Cancelled, true, [1]));
        var handler = new StartVisitCommandHandler(new InlineUnitOfWork(), _appointments, _visits);

        var act = () => handler.HandleAsync(new StartVisitCommand(5, 1), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
        await _visits.DidNotReceive().AddAsync(Arg.Any<Visit>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartVisitHandleAsync_UnknownAppointment_ThrowsNotFound()
    {
        _appointments.FindActiveAsync(99, Arg.Any<CancellationToken>()).Returns((Appointment?)null);
        var handler = new StartVisitCommandHandler(new InlineUnitOfWork(), _appointments, _visits);

        var act = () => handler.HandleAsync(new StartVisitCommand(99, 1), CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
    }
}
