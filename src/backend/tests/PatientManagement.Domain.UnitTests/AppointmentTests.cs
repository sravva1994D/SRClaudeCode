using FluentAssertions;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Domain.UnitTests;

public sealed class AppointmentTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Schedule_FutureQuarterHour_CreatesScheduledAppointment()
    {
        var appointment = Appointment.Schedule(7, new DateTime(2026, 11, 11, 10, 15, 0, DateTimeKind.Utc), Now);

        appointment.StatusId.Should().Be(AppointmentStatuses.Scheduled);
        appointment.IsActive.Should().BeTrue();
        appointment.PatientId.Should().Be(7);
    }

    [Theory]
    [InlineData(10, 7)]
    [InlineData(10, 30)]
    [InlineData(10, 45)]
    public void Schedule_StartNotOnQuarterHour_ThrowsWithCode(int hour, int minute)
    {
        var act = () => Appointment.Schedule(7, new DateTime(2026, 11, 11, hour, minute, 0, DateTimeKind.Utc).AddMinutes(1), Now);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("slot_not_aligned");
    }

    [Fact]
    public void Schedule_SlotAtTheCurrentTime_ThrowsAppointmentInPast()
    {
        var act = () => Appointment.Schedule(7, Now, Now);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("appointment_in_past");
    }

    [Fact]
    public void Schedule_SlotBeforeTheCurrentTime_ThrowsAppointmentInPast()
    {
        var act = () => Appointment.Schedule(7, new DateTime(2026, 11, 11, 9, 0, 0, DateTimeKind.Utc), Now);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("appointment_in_past");
    }
}
