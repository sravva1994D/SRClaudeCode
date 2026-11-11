using Microsoft.Data.SqlClient;

namespace PatientManagement.Infrastructure.IntegrationTests.Support;

/// <summary>A uniquely named LocalDB database for one test. It is dropped when the test finishes.</summary>
public sealed class TestDatabase : IAsyncDisposable
{
    private const string ServerConnectionString =
        "Server=(LocalDb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

    private TestDatabase(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public string ConnectionString =>
        new SqlConnectionStringBuilder(ServerConnectionString) { InitialCatalog = Name }.ConnectionString;

    public static async Task<TestDatabase> CreateAsync()
    {
        var database = new TestDatabase($"PatientManagement_Test_{Guid.NewGuid():N}");
        await using var connection = new SqlConnection(ServerConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        // The name is generated above from a GUID, never from input, so it cannot inject SQL.
#pragma warning disable CA2100
        command.CommandText = $"CREATE DATABASE [{database.Name}];";
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
        return database;
    }

    public async ValueTask DisposeAsync()
    {
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(ServerConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

#pragma warning disable CA2100
        command.CommandText =
            $"IF DB_ID(N'{Name}') IS NOT NULL BEGIN ALTER DATABASE [{Name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{Name}]; END";
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }
}
