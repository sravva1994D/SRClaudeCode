using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Appointments;
using PatientManagement.Application.Appointments.Handlers;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Validators;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Application.UnitTests;

public sealed class AppointmentChangeHandlersTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    private readonly IAppointmentRepository _appointments = Substitute.For<IAppointmentRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public AppointmentChangeHandlersTests()
    {
        _clock.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task RescheduleHandleAsync_ScheduledAppointment_SavesTheNewSlotAsAnUpdate()
    {
        var appointment = Appointment.Reconstitute(5, 7, new DateTime(2026, 11, 11, 11, 0, 0, DateTimeKind.Utc), AppointmentStatuses.Scheduled, true, [1]);
        _appointments.FindActiveAsync(5, Arg.Any<CancellationToken>()).Returns(appointment);
        var handler = new RescheduleAppointmentCommandHandler(new InlineUnitOfWork(), _appointments, _clock);

        var result = await handler.HandleAsync(
            new RescheduleAppointmentCommand(5, new DateTime(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc), 1),
            CancellationToken.None);

        result.SlotStartUtc.Should().Be(new DateTime(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc));
        await _appointments.Received(1).SaveAsync(
            appointment,
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            AuditAction.Update,
            1,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RescheduleHandleAsync_UnknownAppointment_ThrowsAndSavesNothing()
    {
        _appointments.FindActiveAsync(99, Arg.Any<CancellationToken>()).Returns((Appointment?)null);
        var handler = new RescheduleAppointmentCommandHandler(new InlineUnitOfWork(), _appointments, _clock);

        var act = () => handler.HandleAsync(
            new RescheduleAppointmentCommand(99, new DateTime(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc), 1),
            CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
        await _appointments.DidNotReceive().SaveAsync(
            Arg.Any<Appointment>(), Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<AuditAction>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeStatusHandleAsync_ScheduledAppointment_SavesTheOutcomeAsAnUpdate()
    {
        var appointment = Appointment.Reconstitute(5, 7, new DateTime(2026, 11, 11, 11, 0, 0, DateTimeKind.Utc), AppointmentStatuses.Scheduled, true, [1]);
        _appointments.FindActiveAsync(5, Arg.Any<CancellationToken>()).Returns(appointment);
        var handler = new ChangeAppointmentStatusCommandHandler(new InlineUnitOfWork(), _appointments);

        var result = await handler.HandleAsync(
            new ChangeAppointmentStatusCommand(5, AppointmentStatuses.Cancelled, 1),
            CancellationToken.None);

        result.StatusId.Should().Be(AppointmentStatuses.Cancelled);
        await _appointments.Received(1).SaveAsync(
            appointment,
            Arg.Any<IReadOnlyDictionary<string, object?>>(),
            AuditAction.Update,
            1,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RescheduleValidator_MissingSlot_HasSlotError()
    {
        var result = new RescheduleAppointmentCommandValidator().Validate(
            new RescheduleAppointmentCommand(5, default, 1));

        result.Errors.Should().Contain(error => error.PropertyName == nameof(RescheduleAppointmentCommand.SlotStartUtc));
    }
}
