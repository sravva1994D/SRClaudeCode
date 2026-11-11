namespace PatientManagement.Domain.Users;

/// <summary>Lockout rules from CLAUDE.md 10.2. They are fixed business rules, not settings.</summary>
public static class LoginLockoutRules
{
    /// <summary>Failed attempts that lock the account.</summary>
    public const int FailureThreshold = 5;

    /// <summary>Window in which failures are counted.</summary>
    public static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);

    /// <summary>How long the account stays locked after the threshold is reached.</summary>
    public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);
}
