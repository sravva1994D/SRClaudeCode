using FluentAssertions;
using PatientManagement.Application.Appointments;
using PatientManagement.Application.Validators;

namespace PatientManagement.Application.UnitTests;

public sealed class ListAppointmentsValidatorTests
{
    private static readonly DateTime Start = new(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Validate_OneWeekRange_HasNoErrors()
    {
        var result = new ListAppointmentsQueryValidator().Validate(new ListAppointmentsQuery(Start, Start.AddDays(7)));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EndBeforeStart_HasAnError()
    {
        var result = new ListAppointmentsQueryValidator().Validate(new ListAppointmentsQuery(Start, Start.AddDays(-1)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_RangeLongerThanTheMaximum_HasAnError()
    {
        var result = new ListAppointmentsQueryValidator().Validate(
            new ListAppointmentsQuery(Start, Start.AddDays(ListAppointmentsQueryValidator.MaximumRangeDays + 1)));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MissingTimes_HasAnError()
    {
        var result = new ListAppointmentsQueryValidator().Validate(new ListAppointmentsQuery(default, default));

        result.IsValid.Should().BeFalse();
    }
}
