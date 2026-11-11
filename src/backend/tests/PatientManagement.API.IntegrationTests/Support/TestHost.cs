using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PatientManagement.Infrastructure.Migrations;
using PatientManagement.Infrastructure.Persistence;
using PatientManagement.Infrastructure.Seeding;
using PatientManagement.Infrastructure.Security;

namespace PatientManagement.API.IntegrationTests.Support;

/// <summary>Builds test hosts with a generated signing key and optional migrated database.</summary>
public static class TestHost
{
    public const string ReachableConnectionString =
        "Server=(LocalDb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

    public static readonly string SigningKeyPem = CreateSigningKeyPem();

    /// <summary>
    /// Builds a host. Migrations are not applied at startup here: the test calls <see cref="TemporaryDatabase.MigrateAsync"/>
    /// first, because the host can accept requests before startup migrations finish.
    /// </summary>
    public static WebApplicationFactory<Program> With(
        WebApplicationFactory<Program> factory,
        string environment,
        string connectionString) =>
        factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
            builder.UseSetting("Database:ConnectionString", connectionString);
            builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
            builder.UseSetting("Auth:SigningKeyPem", SigningKeyPem);
            builder.ConfigureTestServices(services =>
                services.AddControllers().AddApplicationPart(typeof(TestFailuresController).Assembly));
        });

    private static string CreateSigningKeyPem()
    {
        using var rsa = RSA.Create(2048);
        return rsa.ExportRSAPrivateKeyPem();
    }
}

/// <summary>A database created for one test class and dropped afterwards. The test migrates and seeds it first.</summary>
public sealed class TemporaryDatabase : IAsyncDisposable
{
    private const string ServerConnectionString = TestHost.ReachableConnectionString;

    private TemporaryDatabase(string name) => Name = name;

    public string Name { get; }

    public string ConnectionString =>
        new SqlConnectionStringBuilder(ServerConnectionString) { InitialCatalog = Name }.ConnectionString;

    public static async Task<TemporaryDatabase> CreateAsync()
    {
        var database = new TemporaryDatabase($"PatientManagement_ApiTest_{Guid.NewGuid():N}");
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

    /// <summary>Applies the same embedded migrations the application ships with.</summary>
    public async Task MigrateAsync()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new DatabaseOptions { ConnectionString = ConnectionString });
        var scripts = EmbeddedMigrationScripts.Load(typeof(MigrationRunner).Assembly);
        await new MigrationRunner(new SqlConnectionFactory(options), options, NullLogger<MigrationRunner>.Instance)
            .ApplyAsync(scripts, CancellationToken.None);
    }

    /// <summary>Creates the synthetic doctor account so the auth tests have a user to sign in with.</summary>
    public async Task SeedDoctorAsync(string password)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new DatabaseOptions { ConnectionString = ConnectionString });
        await new SyntheticDataSeeder(new SqlConnectionFactory(options))
            .SeedAsync(new BcryptPasswordHasher().Hash(password), CancellationToken.None);
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
