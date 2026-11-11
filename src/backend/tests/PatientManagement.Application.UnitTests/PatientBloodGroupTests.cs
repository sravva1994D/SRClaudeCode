using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Patients;
using PatientManagement.Application.Patients.Handlers;
using PatientManagement.Application.Validators;

namespace PatientManagement.Application.UnitTests;

public sealed class PatientBloodGroupTests
{
    private static readonly DateOnly DateOfBirth = new(1990, 5, 14);

    [Fact]
    public void Validate_BloodGroupInSeededRange_HasNoErrors()
    {
        var result = new CreatePatientCommandValidator().Validate(
            new CreatePatientCommand(Fields(bloodGroupId: 9), 1));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Validate_BloodGroupOutsideSeededRange_HasBloodGroupError(int bloodGroupId)
    {
        var result = new CreatePatientCommandValidator().Validate(
            new CreatePatientCommand(Fields(bloodGroupId: bloodGroupId), 1));

        result.Errors.Should().Contain(error => error.PropertyName == "Fields.BloodGroupId");
    }

    [Fact]
    public void Validate_ChronicConditionsOverLimit_HasChronicConditionsError()
    {
        var result = new CreatePatientCommandValidator().Validate(
            new CreatePatientCommand(Fields(chronicConditions: new string('a', 1001)), 1));

        result.Errors.Should().Contain(error => error.PropertyName == "Fields.ChronicConditions");
    }

    [Fact]
    public async Task CreateHandleAsync_BlankChronicConditions_StoresNull()
    {
        var patients = Substitute.For<IPatientRepository>();
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(new DateTime(2026, 11, 11, 10, 0, 0, DateTimeKind.Utc));
        var handler = new CreatePatientCommandHandler(new InlineUnitOfWork(), patients, clock);

        await handler.HandleAsync(new CreatePatientCommand(Fields(bloodGroupId: 3, chronicConditions: "  "), 1), CancellationToken.None);

        await patients.Received(1).AddAsync(
            Arg.Is<PatientManagement.Domain.Patients.Patient>(
                patient => patient.BloodGroupId == 3 && patient.ChronicConditions == null),
            1,
            Arg.Any<CancellationToken>());
    }

    private static PatientFields Fields(int? bloodGroupId = null, string? chronicConditions = null) =>
        new("Asha Rao", DateOfBirth, 2, "9876543210", null, bloodGroupId, chronicConditions);
}
