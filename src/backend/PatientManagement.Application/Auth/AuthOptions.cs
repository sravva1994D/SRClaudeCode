namespace PatientManagement.Application.Auth;

/// <summary>Settings for the "Auth" configuration section (CLAUDE.md 6.8, 10.1). The signing key is a secret.</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>RSA private key in PEM form. Development: user secrets. Never in source files.</summary>
    public string SigningKeyPem { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Absolute lifetime of a login session (one refresh-token family).</summary>
    public int RefreshTokenHours { get; set; } = 8;

    /// <summary>A refresh is refused when the token has been unused for longer than this.</summary>
    public int IdleTimeoutMinutes { get; set; } = 30;
}
