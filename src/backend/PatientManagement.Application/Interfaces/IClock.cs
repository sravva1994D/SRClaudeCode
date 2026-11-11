namespace PatientManagement.Application.Interfaces;

/// <summary>The only source of time for application code (CLAUDE.md 6.4). Always UTC.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
