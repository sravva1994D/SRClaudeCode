namespace PatientManagement.Infrastructure.Migrations;

/// <summary>Raised when migrations cannot be applied safely. Messages name the script, never its contents.</summary>
public sealed class MigrationException(string message, Exception? innerException = null)
    : Exception(message, innerException);
