using System.Data.Common;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Creates open database connections. Connections are created only through this interface.</summary>
public interface ISqlConnectionFactory
{
    Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken);
}
