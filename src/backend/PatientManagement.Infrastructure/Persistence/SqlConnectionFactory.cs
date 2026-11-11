using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Opens SQL Server connections from the configured database options.</summary>
public sealed class SqlConnectionFactory(IOptions<DatabaseOptions> options) : ISqlConnectionFactory
{
    public async Task<DbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(options.Value.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
