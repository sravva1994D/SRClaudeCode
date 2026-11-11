using FluentAssertions;
using PatientManagement.Domain.Security;

namespace PatientManagement.Domain.UnitTests;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("Passw0rd!")]
    [InlineData("Correct-Horse-9")]
    public void IsSatisfiedBy_MeetsEveryRule_ReturnsTrue(string password)
    {
        PasswordPolicy.IsSatisfiedBy(password).Should().BeTrue();
    }

    [Theory]
    [InlineData("Pass0r!")]
    [InlineData("")]
    public void IsSatisfiedBy_ShorterThanEightCharacters_ReturnsFalse(string password)
    {
        PasswordPolicy.IsSatisfiedBy(password).Should().BeFalse();
    }

    [Theory]
    [InlineData("passw0rd!")]
    [InlineData("PASSW0RD!")]
    [InlineData("Password!")]
    [InlineData("Passw0rd1")]
    public void IsSatisfiedBy_MissingOneCharacterClass_ReturnsFalse(string password)
    {
        PasswordPolicy.IsSatisfiedBy(password).Should().BeFalse();
    }

    [Fact]
    public void IsSatisfiedBy_NullPassword_ThrowsArgumentNullException()
    {
        var act = () => PasswordPolicy.IsSatisfiedBy(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
