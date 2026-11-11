using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PatientManagement.Infrastructure.Migrations;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests.Support;

/// <summary>Applies the same embedded scripts the application ships with, to one test database.</summary>
public static class TestMigrations
{
    public static async Task ApplyAllAsync(TestDatabase database)
    {
        var scripts = EmbeddedMigrationScripts.Load(typeof(MigrationRunner).Assembly);
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var runner = new MigrationRunner(
            new SqlConnectionFactory(options),
            options,
            NullLogger<MigrationRunner>.Instance);

        await runner.ApplyAsync(scripts, CancellationToken.None);
    }
}
