using FluentAssertions;
using PatientManagement.Domain.Appointments;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Visits;

namespace PatientManagement.Domain.UnitTests;

public sealed class VisitTests
{
    private static readonly DateTime SlotStart = new(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Start_NewVisit_IsInProgressAndLinksTheAppointment()
    {
        var visit = Visit.Start(7, 5);

        visit.StatusId.Should().Be(VisitStatuses.InProgress);
        visit.PatientId.Should().Be(7);
        visit.AppointmentId.Should().Be(5);
    }

    [Fact]
    public void EnsureCanStartVisit_ScheduledAppointment_IsAllowed()
    {
        var appointment = Appointment.Reconstitute(5, 7, SlotStart, AppointmentStatuses.Scheduled, true, [1]);

        var act = () => appointment.EnsureCanStartVisit();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(AppointmentStatuses.Cancelled)]
    [InlineData(AppointmentStatuses.Completed)]
    [InlineData(AppointmentStatuses.NoShow)]
    public void EnsureCanStartVisit_AppointmentWithOutcome_ThrowsNotStartable(int statusId)
    {
        var appointment = Appointment.Reconstitute(5, 7, SlotStart, statusId, true, [1]);

        var act = () => appointment.EnsureCanStartVisit();

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("appointment_not_startable");
    }
}
