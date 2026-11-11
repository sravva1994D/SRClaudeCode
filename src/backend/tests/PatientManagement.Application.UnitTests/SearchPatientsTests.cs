using FluentAssertions;
using PatientManagement.Application.Patients.Handlers;
using PatientManagement.Application.Patients;
using PatientManagement.Application.Validators;

namespace PatientManagement.Application.UnitTests;

public sealed class SearchPatientsTests
{
    [Theory]
    [InlineData("Asha", "Asha", null)]
    [InlineData("  Asha Rao  ", "Asha Rao", null)]
    [InlineData("98765 43210", null, "9876543210")]
    [InlineData("+91 (98) 765-43210", null, "919876543210")]
    [InlineData("Asha 2", "Asha 2", null)]
    public void Classify_SearchText_SearchesNameOrPhone(string searchText, string? expectedName, string? expectedDigits)
    {
        var result = SearchPatientsQueryHandler.Classify(searchText);

        result.NameTerm.Should().Be(expectedName);
        result.PhoneDigits.Should().Be(expectedDigits);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("+-()")]
    public void Classify_BlankOrSymbolsOnly_SearchesNothing(string? searchText)
    {
        var result = SearchPatientsQueryHandler.Classify(searchText);

        result.NameTerm.Should().BeNull();
        result.PhoneDigits.Should().BeNull();
    }

    [Fact]
    public void Validate_PageSizeAboveMaximum_HasPageSizeError()
    {
        var result = new SearchPatientsQueryValidator().Validate(new SearchPatientsQuery(null, 1, 101));

        result.Errors.Should().Contain(error => error.PropertyName == nameof(SearchPatientsQuery.PageSize));
    }

    [Fact]
    public void Validate_PageNumberZero_HasPageNumberError()
    {
        var result = new SearchPatientsQueryValidator().Validate(new SearchPatientsQuery(null, 0, 20));

        result.Errors.Should().Contain(error => error.PropertyName == nameof(SearchPatientsQuery.PageNumber));
    }

    [Fact]
    public void Validate_SearchTextTooLong_HasSearchTextError()
    {
        var result = new SearchPatientsQueryValidator().Validate(new SearchPatientsQuery(new string('a', 101), 1, 20));

        result.Errors.Should().Contain(error => error.PropertyName == nameof(SearchPatientsQuery.SearchText));
    }

    [Fact]
    public void Validate_DefaultPaging_HasNoErrors()
    {
        var result = new SearchPatientsQueryValidator().Validate(new SearchPatientsQuery("Asha", 1, 20));

        result.IsValid.Should().BeTrue();
    }
}
