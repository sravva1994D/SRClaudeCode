namespace PatientManagement.Application.Auth;

/// <summary>What a successful login or refresh returns. The refresh token is sent to the browser once, as a cookie.</summary>
public sealed record SessionTokens(
    string AccessToken,
    DateTime AccessTokenExpiresUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresUtc);

public sealed record LoginCommand(string Username, string Password);

public sealed record RefreshSessionCommand(string RefreshToken);

public sealed record LogoutCommand(string RefreshToken);

public sealed record ChangePasswordCommand(int UserId, string CurrentPassword, string NewPassword);

/// <summary>Operator command: sets a new password without the old one (plan step 17). Revokes all sessions.</summary>
public sealed record ResetPasswordCommand(string Username, string NewPassword);
