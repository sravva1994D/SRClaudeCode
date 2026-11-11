namespace PatientManagement.Application.Auth;

/// <summary>User-safe messages and codes for authentication failures. They never say which part was wrong.</summary>
public static class AuthMessages
{
    public const string InvalidCredentialsCode = "invalid_credentials";
    public const string InvalidCredentialsMessage = "Invalid username or password.";

    public const string SessionEndedCode = "session_ended";
    public const string SessionEndedMessage = "Your session has ended. Sign in again.";

    public const string CurrentPasswordIncorrectCode = "current_password_incorrect";
    public const string CurrentPasswordIncorrectMessage = "The current password is incorrect.";

    public const string WeakPasswordCode = "weak_password";
    public const string WeakPasswordMessage =
        "The new password needs at least 8 characters, with upper-case, lower-case, a digit and a symbol.";

    public const string UnknownUserCode = "unknown_user";
    public const string UnknownUserMessage = "No active account has that username.";
}
