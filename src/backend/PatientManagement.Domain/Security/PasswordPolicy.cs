namespace PatientManagement.Domain.Security;

/// <summary>Password rules from CLAUDE.md 10.2: at least 8 characters with upper case, lower case, a digit and a symbol.</summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 8;

    public static bool IsSatisfiedBy(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        return password.Length >= MinimumLength
            && password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(character => !char.IsLetterOrDigit(character));
    }
}
