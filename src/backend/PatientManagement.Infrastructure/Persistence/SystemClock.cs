using PatientManagement.Application.Interfaces;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Reads the system clock in UTC.</summary>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
