using FluentAssertions;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Domain.UnitTests;

public sealed class AppointmentChangeTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Reschedule_ScheduledAppointmentToFutureQuarterHour_MovesTheSlot()
    {
        var appointment = Appointment.Schedule(7, new DateTime(2026, 11, 11, 11, 0, 0, DateTimeKind.Utc), Now);

        appointment.Reschedule(new DateTime(2026, 11, 11, 12, 15, 0, DateTimeKind.Utc), Now);

        appointment.SlotStartUtc.Should().Be(new DateTime(2026, 11, 11, 12, 15, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Reschedule_CompletedAppointment_ThrowsNotReschedulable()
    {
        var appointment = Appointment.Reconstitute(1, 7, new DateTime(2026, 11, 11, 11, 0, 0, DateTimeKind.Utc), AppointmentStatuses.Completed, true, [1]);

        var act = () => appointment.Reschedule(new DateTime(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc), Now);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("appointment_not_reschedulable");
    }

    [Fact]
    public void Reschedule_ToPastSlot_ThrowsAppointmentInPast()
    {
        var appointment = Appointment.Schedule(7, new DateTime(2026, 11, 11, 11, 0, 0, DateTimeKind.Utc), Now);

        var act = () => appointment.Reschedule(new DateTime(2026, 11, 11, 9, 0, 0, DateTimeKind.Utc), Now);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("appointment_in_past");
    }

    [Fact]
    public void Reschedule_ToSlotNotOnQuarterHour_ThrowsSlotNotAligned()
    {
        var appointment = Appointment.Schedule(7, new DateTime(2026, 11, 11, 11, 0, 0, DateTimeKind.Utc), Now);

        var act = () => appointment.Reschedule(new DateTime(2026, 11, 11, 12, 7, 0, DateTimeKind.Utc), Now);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("slot_not_aligned");
    }

    [Theory]
    [InlineData(AppointmentStatuses.Completed)]
    [InlineData(AppointmentStatuses.Cancelled)]
    [InlineData(AppointmentStatuses.NoShow)]
    public void ChangeStatus_ScheduledAppointment_SetsTheOutcome(int target)
    {
        var appointment = Appointment.Schedule(7, new DateTime(2026, 11, 11, 11, 0, 0, DateTimeKind.Utc), Now);

        appointment.ChangeStatus(target);

        appointment.StatusId.Should().Be(target);
    }

    [Fact]
    public void ChangeStatus_AlreadyCancelled_ThrowsStatusLocked()
    {
        var appointment = Appointment.Reconstitute(1, 7, new DateTime(2026, 11, 11, 11, 0, 0, DateTimeKind.Utc), AppointmentStatuses.Cancelled, true, [1]);

        var act = () => appointment.ChangeStatus(AppointmentStatuses.Completed);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("appointment_status_locked");
    }

    [Fact]
    public void ChangeStatus_ToScheduled_ThrowsInvalidTransition()
    {
        var appointment = Appointment.Schedule(7, new DateTime(2026, 11, 11, 11, 0, 0, DateTimeKind.Utc), Now);

        var act = () => appointment.ChangeStatus(AppointmentStatuses.Scheduled);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("invalid_status_transition");
    }
}
