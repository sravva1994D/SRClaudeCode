using FluentAssertions;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Patients;

namespace PatientManagement.Domain.UnitTests;

public sealed class PatientTests
{
    private static readonly DateOnly Today = new(2026, 11, 11);

    [Fact]
    public void Register_ValidDetails_CreatesActivePatient()
    {
        var patient = Patient.Register("Asha Rao", new DateOnly(1990, 5, 14), 2, "9876543210", "12 Lake Road", Today);

        patient.IsActive.Should().BeTrue();
        patient.FullName.Should().Be("Asha Rao");
    }

    [Fact]
    public void Register_DateOfBirthInFuture_ThrowsWithCode()
    {
        var act = () => Patient.Register("Asha Rao", Today.AddDays(1), 2, null, null, Today);

        act.Should().Throw<DomainException>().Which.ErrorCode.Should().Be("date_of_birth_in_future");
    }

    [Fact]
    public void Update_DateOfBirthInFuture_ThrowsAndKeepsOldValues()
    {
        var patient = Patient.Register("Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, Today);

        var act = () => patient.Update("Asha Rao", Today.AddDays(1), 2, null, null, Today);

        act.Should().Throw<DomainException>();
        patient.DateOfBirth.Should().Be(new DateOnly(1990, 5, 14));
    }

    [Fact]
    public void Deactivate_ActivePatient_BecomesInactive()
    {
        var patient = Patient.Register("Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, Today);

        patient.Deactivate();

        patient.IsActive.Should().BeFalse();
    }

    [Fact]
    public void AuditValues_DateOfBirth_IsIsoDate()
    {
        var patient = Patient.Register("Asha Rao", new DateOnly(1990, 5, 14), 2, null, null, Today);

        patient.AuditValues()["DateOfBirth"].Should().Be("1990-05-14");
    }
}
