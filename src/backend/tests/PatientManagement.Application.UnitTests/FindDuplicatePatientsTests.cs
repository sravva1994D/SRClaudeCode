using FluentAssertions;
using NSubstitute;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Patients;
using PatientManagement.Application.Patients.Handlers;
using PatientManagement.Application.Validators;
using PatientManagement.Domain.Patients;

namespace PatientManagement.Application.UnitTests;

public sealed class FindDuplicatePatientsTests
{
    private static readonly DateOnly DateOfBirth = new(1990, 5, 14);

    [Fact]
    public async Task HandleAsync_NameWithSpaces_SearchesTheTrimmedName()
    {
        var patients = Substitute.For<IPatientRepository>();
        patients.FindActiveDuplicatesAsync("Asha Rao", DateOfBirth, 7, 5, Arg.Any<CancellationToken>())
            .Returns(new List<Patient>());
        var handler = new FindDuplicatePatientsQueryHandler(patients);

        await handler.HandleAsync(new FindDuplicatePatientsQuery("  Asha Rao ", DateOfBirth, 7), CancellationToken.None);

        await patients.Received(1).FindActiveDuplicatesAsync("Asha Rao", DateOfBirth, 7, 5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MatchingPatient_ReturnsItsSummaryOnly()
    {
        var patients = Substitute.For<IPatientRepository>();
        var match = Patient.Reconstitute(3, "Asha Rao", DateOfBirth, 2, "919876543210", "12 Lake Road", true, [1]);
        patients.FindActiveDuplicatesAsync(Arg.Any<string>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Patient> { match });
        var handler = new FindDuplicatePatientsQueryHandler(patients);

        var result = await handler.HandleAsync(new FindDuplicatePatientsQuery("Asha Rao", DateOfBirth, null), CancellationToken.None);

        result.Should().ContainSingle()
            .Which.Should().Be(new PatientSummary(3, "Asha Rao", DateOfBirth));
    }

    [Fact]
    public void Validate_EmptyName_HasNameError()
    {
        var validator = new FindDuplicatePatientsQueryValidator();

        var result = validator.Validate(new FindDuplicatePatientsQuery(" ", DateOfBirth, null));

        result.Errors.Should().Contain(error => error.PropertyName == nameof(FindDuplicatePatientsQuery.FullName));
    }

    [Fact]
    public void Validate_DefaultDateOfBirth_HasDateOfBirthError()
    {
        var validator = new FindDuplicatePatientsQueryValidator();

        var result = validator.Validate(new FindDuplicatePatientsQuery("Asha Rao", default, null));

        result.Errors.Should().Contain(error => error.PropertyName == nameof(FindDuplicatePatientsQuery.DateOfBirth));
    }
}
