namespace PatientManagement.Domain.Users;

/// <summary>The doctor's account. Lockout and password state change only through the methods below.</summary>
public sealed class UserAccount
{
    private const string RedactedValue = "[redacted]";

    private UserAccount(
        int userId,
        string username,
        string passwordHash,
        bool isActive,
        int failedLoginCount,
        DateTime? failedLoginWindowStartUtc,
        DateTime? lockedUntilUtc,
        DateTime? lastLoginUtc,
        DateTime? passwordChangedUtc,
        byte[] rowVersion)
    {
        UserId = userId;
        Username = username;
        PasswordHash = passwordHash;
        IsActive = isActive;
        FailedLoginCount = failedLoginCount;
        FailedLoginWindowStartUtc = failedLoginWindowStartUtc;
        LockedUntilUtc = lockedUntilUtc;
        LastLoginUtc = lastLoginUtc;
        PasswordChangedUtc = passwordChangedUtc;
        RowVersion = rowVersion;
    }

    public int UserId { get; }

    public string Username { get; }

    public string PasswordHash { get; private set; }

    public bool IsActive { get; }

    public int FailedLoginCount { get; private set; }

    public DateTime? FailedLoginWindowStartUtc { get; private set; }

    public DateTime? LockedUntilUtc { get; private set; }

    public DateTime? LastLoginUtc { get; private set; }

    public DateTime? PasswordChangedUtc { get; private set; }

    public byte[] RowVersion { get; private set; }

    /// <summary>Rebuilds the account from storage. Does not apply any rule.</summary>
    public static UserAccount Reconstitute(
        int userId,
        string username,
        string passwordHash,
        bool isActive,
        int failedLoginCount,
        DateTime? failedLoginWindowStartUtc,
        DateTime? lockedUntilUtc,
        DateTime? lastLoginUtc,
        DateTime? passwordChangedUtc,
        byte[] rowVersion) =>
        new(
            userId,
            username,
            passwordHash,
            isActive,
            failedLoginCount,
            failedLoginWindowStartUtc,
            lockedUntilUtc,
            lastLoginUtc,
            passwordChangedUtc,
            rowVersion);

    public bool IsLockedAt(DateTime nowUtc) => LockedUntilUtc is { } lockedUntil && lockedUntil > nowUtc;

    /// <summary>Counts a failed attempt. Reaching the threshold inside the window locks the account.</summary>
    public void RecordFailedLogin(DateTime nowUtc)
    {
        var windowStillOpen = FailedLoginWindowStartUtc is { } windowStart
            && nowUtc - windowStart <= LoginLockoutRules.FailureWindow;

        if (windowStillOpen)
        {
            FailedLoginCount++;
        }
        else
        {
            FailedLoginWindowStartUtc = nowUtc;
            FailedLoginCount = 1;
        }

        if (FailedLoginCount >= LoginLockoutRules.FailureThreshold)
        {
            LockedUntilUtc = nowUtc + LoginLockoutRules.LockDuration;
            FailedLoginCount = 0;
            FailedLoginWindowStartUtc = null;
        }
    }

    public void RecordSuccessfulLogin(DateTime nowUtc)
    {
        FailedLoginCount = 0;
        FailedLoginWindowStartUtc = null;
        LockedUntilUtc = null;
        LastLoginUtc = nowUtc;
    }

    public void ChangePassword(string newPasswordHash, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);

        PasswordHash = newPasswordHash;
        PasswordChangedUtc = nowUtc;
    }

    public void SetRowVersion(byte[] rowVersion) => RowVersion = rowVersion;

    /// <summary>The values an audit row records. The password hash is redacted: it never leaves the database.</summary>
    public IReadOnlyDictionary<string, object?> AuditValues() => new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["PasswordHash"] = RedactedValue,
        ["FailedLoginCount"] = FailedLoginCount,
        ["FailedLoginWindowStartDate"] = FailedLoginWindowStartUtc,
        ["LockedUntilDate"] = LockedUntilUtc,
        ["LastLoginDate"] = LastLoginUtc,
        ["PasswordChangedDate"] = PasswordChangedUtc,
    };
}
