namespace PatientManagement.Domain.Auth;

/// <summary>
/// One refresh token. A login starts a family; each use revokes the token and issues the next one in the same family.
/// Only the token's hash is stored. The absolute expiry is fixed for the whole family.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>Revocation reasons written to the table and the audit log.</summary>
    public static class RevocationReasons
    {
        public const string Rotated = "rotated";
        public const string Logout = "logout";
        public const string PasswordChanged = "password_changed";
        public const string ReuseDetected = "reuse_detected";
        public const string IdleTimeout = "idle_timeout";
        public const string Expired = "expired";
        public const string AccountInactive = "account_inactive";
    }

    private RefreshToken(
        int refreshTokenId,
        int userId,
        Guid familyId,
        string tokenHash,
        DateTime issuedUtc,
        DateTime expiresUtc,
        DateTime lastUsedUtc,
        DateTime? revokedUtc,
        string? revokedReason,
        byte[] rowVersion)
    {
        RefreshTokenId = refreshTokenId;
        UserId = userId;
        FamilyId = familyId;
        TokenHash = tokenHash;
        IssuedUtc = issuedUtc;
        ExpiresUtc = expiresUtc;
        LastUsedUtc = lastUsedUtc;
        RevokedUtc = revokedUtc;
        RevokedReason = revokedReason;
        RowVersion = rowVersion;
    }

    /// <summary>0 until the database assigns the id on insert.</summary>
    public int RefreshTokenId { get; private set; }

    public int UserId { get; }

    public Guid FamilyId { get; }

    public string TokenHash { get; }

    public DateTime IssuedUtc { get; }

    /// <summary>Absolute limit for the whole family: eight hours after the login.</summary>
    public DateTime ExpiresUtc { get; }

    public DateTime LastUsedUtc { get; private set; }

    public DateTime? RevokedUtc { get; private set; }

    public string? RevokedReason { get; private set; }

    public byte[] RowVersion { get; private set; }

    public bool IsRevoked => RevokedUtc is not null;

    /// <summary>Starts a new family at login.</summary>
    public static RefreshToken Issue(int userId, Guid familyId, string tokenHash, DateTime nowUtc, DateTime expiresUtc) =>
        new(0, userId, familyId, tokenHash, nowUtc, expiresUtc, nowUtc, null, null, []);

    /// <summary>Creates the next token of an existing family. The family keeps its absolute expiry.</summary>
    public static RefreshToken Continue(RefreshToken previous, string tokenHash, DateTime nowUtc) =>
        new(0, previous.UserId, previous.FamilyId, tokenHash, nowUtc, previous.ExpiresUtc, nowUtc, null, null, []);

    /// <summary>Rebuilds the token from storage. Does not apply any rule.</summary>
    public static RefreshToken Reconstitute(
        int refreshTokenId,
        int userId,
        Guid familyId,
        string tokenHash,
        DateTime issuedUtc,
        DateTime expiresUtc,
        DateTime lastUsedUtc,
        DateTime? revokedUtc,
        string? revokedReason,
        byte[] rowVersion) =>
        new(refreshTokenId, userId, familyId, tokenHash, issuedUtc, expiresUtc, lastUsedUtc, revokedUtc, revokedReason, rowVersion);

    public void SetRefreshTokenId(int refreshTokenId) => RefreshTokenId = refreshTokenId;

    public void SetRowVersion(byte[] rowVersion) => RowVersion = rowVersion;

    public bool IsExpiredAt(DateTime nowUtc) => nowUtc >= ExpiresUtc;

    public bool IsIdleAt(DateTime nowUtc, TimeSpan idleTimeout) => nowUtc - LastUsedUtc > idleTimeout;

    public void Revoke(DateTime nowUtc, string reason)
    {
        if (RevokedUtc is not null)
        {
            return;
        }

        RevokedUtc = nowUtc;
        RevokedReason = reason;
    }

    public IReadOnlyDictionary<string, object?> AuditValues() => new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["LastUsedDate"] = LastUsedUtc,
        ["RevokedDate"] = RevokedUtc,
        ["RevokedReason"] = RevokedReason,
    };
}
