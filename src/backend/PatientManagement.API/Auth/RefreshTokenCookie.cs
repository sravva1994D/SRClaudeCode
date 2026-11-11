using PatientManagement.Application.Auth;

namespace PatientManagement.API.Auth;

/// <summary>
/// The refresh token cookie: HTTP-only, Secure, SameSite=Strict, sent only to the auth endpoints (CLAUDE.md 10.1).
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "pm_refresh";

    public const string Path = "/api/v1/auth";

    public static void Set(HttpResponse response, SessionTokens session) =>
        response.Cookies.Append(Name, session.RefreshToken, Options(session.RefreshTokenExpiresUtc));

    public static void Clear(HttpResponse response) =>
        response.Cookies.Delete(Name, Options(null));

    private static CookieOptions Options(DateTime? expiresUtc) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expiresUtc is { } expires ? new DateTimeOffset(expires, TimeSpan.Zero) : null,
    };
}
