namespace PatientManagement.Domain.Exceptions;

/// <summary>
/// Base type for business-rule failures. The API maps it to 422 with a stable <see cref="ErrorCode"/>.
/// The message must be safe to show to the user: never include patient data or SQL text.
/// </summary>
public class DomainException(string errorCode, string message) : Exception(message)
{
    /// <summary>Stable, machine-readable code such as <c>appointment_in_past</c>. Clients may depend on it.</summary>
    public string ErrorCode { get; } = errorCode;
}
