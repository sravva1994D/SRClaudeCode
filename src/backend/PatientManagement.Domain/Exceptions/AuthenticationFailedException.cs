namespace PatientManagement.Domain.Exceptions;

/// <summary>
/// The caller is not authenticated. The API maps it to 401. Login failures always use the generic message, so a
/// response never shows whether a username exists or is locked (CLAUDE.md 10.2).
/// </summary>
public sealed class AuthenticationFailedException(string errorCode, string message)
    : DomainException(errorCode, message);
