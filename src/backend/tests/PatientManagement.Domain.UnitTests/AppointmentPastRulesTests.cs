using FluentAssertions;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;

namespace PatientManagement.Domain.UnitTests;

public sealed class AppointmentPastRulesTests
{
    private static readonly DateTime SlotStart = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime AfterSlot = new(2026, 11, 10, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Reschedule_ScheduledAppointmentWhoseSlotHasPassed_ThrowsTimeLocked()
    {
        var appointment = Appointment.Reconstitute(1, 7, SlotStart, AppointmentStatuses.Scheduled, true, [1]);

        var act = () => appointment.Reschedule(new DateTime(2026, 11, 12, 9, 0, 0, DateTimeKind.Utc), AfterSlot);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("appointment_time_locked");
    }

    [Fact]
    public void Reschedule_ScheduledAppointmentBeforeItsSlot_IsStillAllowed()
    {
        var appointment = Appointment.Reconstitute(1, 7, SlotStart, AppointmentStatuses.Scheduled, true, [1]);
        var before = new DateTime(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc);

        appointment.Reschedule(new DateTime(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc), before);

        appointment.SlotStartUtc.Should().Be(new DateTime(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ChangeStatus_ScheduledAppointmentAfterItsSlot_RecordsTheOutcome()
    {
        var appointment = Appointment.Reconstitute(1, 7, SlotStart, AppointmentStatuses.Scheduled, true, [1]);

        appointment.ChangeStatus(AppointmentStatuses.Completed);

        appointment.StatusId.Should().Be(AppointmentStatuses.Completed);
        appointment.SlotStartUtc.Should().Be(SlotStart, "a status change must not move the slot");
    }

    [Fact]
    public void ChangeStatus_AfterOutcomeIsSet_ThrowsStatusLocked()
    {
        var appointment = Appointment.Reconstitute(1, 7, SlotStart, AppointmentStatuses.NoShow, true, [1]);

        var act = () => appointment.ChangeStatus(AppointmentStatuses.Completed);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("appointment_status_locked");
    }
}
