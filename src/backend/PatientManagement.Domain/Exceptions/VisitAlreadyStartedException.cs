namespace PatientManagement.Domain.Exceptions;

/// <summary>The appointment already has an active visit. The API maps it to 409 (CLAUDE.md 6.5).</summary>
public sealed class VisitAlreadyStartedException()
    : DomainException("visit_already_started", "A visit has already been started for this appointment.")
{
}
