namespace PatientManagement.API.Auth;

/// <summary>Login request body. Validation runs in the Application layer's validator.</summary>
public sealed record LoginRequest(string Username, string Password);

/// <summary>Password change request body.</summary>
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>
/// The access token returned to the browser. The refresh token is never in the body: it travels only in its
/// HTTP-only cookie (CLAUDE.md 10.1).
/// </summary>
public sealed record AccessTokenResponse(string AccessToken, int ExpiresInSeconds);
