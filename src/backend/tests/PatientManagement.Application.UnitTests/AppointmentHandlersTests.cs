using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Appointments;
using PatientManagement.Application.Appointments.Handlers;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Validators;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;

namespace PatientManagement.Application.UnitTests;

public sealed class AppointmentHandlersTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Slot = new(2026, 11, 11, 10, 15, 0, DateTimeKind.Utc);

    private readonly IPatientRepository _patients = Substitute.For<IPatientRepository>();
    private readonly IAppointmentRepository _appointments = Substitute.For<IAppointmentRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();

    public AppointmentHandlersTests()
    {
        _clock.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task ScheduleHandleAsync_ActivePatient_AddsTheAppointment()
    {
        _patients.FindActiveAsync(7, Arg.Any<CancellationToken>())
            .Returns(Patient.Reconstitute(7, "Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, true, [1]));
        var handler = new ScheduleAppointmentCommandHandler(new InlineUnitOfWork(), _patients, _appointments, _clock);

        var booked = await handler.HandleAsync(new ScheduleAppointmentCommand(7, Slot, 1), CancellationToken.None);

        booked.PatientId.Should().Be(7);
        booked.SlotStartUtc.Should().Be(Slot);
        await _appointments.Received(1).AddAsync(
            Arg.Is<PatientManagement.Domain.Appointments.Appointment>(appointment => appointment.PatientId == 7),
            1,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ScheduleHandleAsync_UnknownPatient_ThrowsAndAddsNothing()
    {
        _patients.FindActiveAsync(99, Arg.Any<CancellationToken>()).Returns((Patient?)null);
        var handler = new ScheduleAppointmentCommandHandler(new InlineUnitOfWork(), _patients, _appointments, _clock);

        var act = () => handler.HandleAsync(new ScheduleAppointmentCommand(99, Slot, 1), CancellationToken.None);

        await act.Should().ThrowAsync<RecordNotFoundException>();
        await _appointments.DidNotReceive().AddAsync(
            Arg.Any<PatientManagement.Domain.Appointments.Appointment>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ScheduleValidator_MissingPatientAndSlot_HasErrors()
    {
        var result = new ScheduleAppointmentCommandValidator().Validate(
            new ScheduleAppointmentCommand(0, default, 1));

        result.Errors.Should().Contain(error => error.PropertyName == nameof(ScheduleAppointmentCommand.PatientId));
        result.Errors.Should().Contain(error => error.PropertyName == nameof(ScheduleAppointmentCommand.SlotStartUtc));
    }
}
