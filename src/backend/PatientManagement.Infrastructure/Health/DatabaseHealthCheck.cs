using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.Health;

/// <summary>Reports whether the database accepts a connection and a trivial query.</summary>
public sealed class DatabaseHealthCheck(
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IHealthCheck
{
    private const string PingSql = "SELECT 1;";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // The exception is deliberately not attached: connection errors can echo the server name.
        try
        {
            await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = PingSql;
            command.CommandTimeout = options.Value.CommandTimeoutSeconds;
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy("The database is not reachable.");
        }
    }
}
