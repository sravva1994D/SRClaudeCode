using FluentAssertions;
using PatientManagement.Application.Patients;
using PatientManagement.Application.Validators;

namespace PatientManagement.Application.UnitTests;

public sealed class PatientValidatorTests
{
    private readonly CreatePatientCommandValidator _validator = new();

    [Fact]
    public void Validate_CompleteDetails_HasNoErrors()
    {
        var result = _validator.Validate(Create(Fields()));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyFullName_ReportsFullNameError()
    {
        var result = _validator.Validate(Create(Fields() with { FullName = "  " }));

        result.Errors.Should().Contain(error => error.PropertyName == "Fields.FullName");
    }

    [Fact]
    public void Validate_MissingGender_ReportsGenderError()
    {
        var result = _validator.Validate(Create(Fields() with { GenderId = 0 }));

        result.Errors.Should().Contain(error => error.PropertyName == "Fields.GenderId");
    }

    [Fact]
    public void Validate_PhoneWithLetters_ReportsPhoneError()
    {
        var result = _validator.Validate(Create(Fields() with { PhoneNumber = "98765 call" }));

        result.Errors.Should().Contain(error => error.PropertyName == "Fields.PhoneNumber");
    }

    [Fact]
    public void Validate_DefaultDateOfBirth_ReportsDateOfBirthError()
    {
        var result = _validator.Validate(Create(Fields() with { DateOfBirth = default }));

        result.Errors.Should().Contain(error => error.PropertyName == "Fields.DateOfBirth");
    }

    private static PatientFields Fields() => new("Asha Rao", new DateOnly(1990, 5, 14), 2, "9876543210", "12 Lake Road");

    private static CreatePatientCommand Create(PatientFields fields) => new(fields, ChangedByUserId: 1);
}
