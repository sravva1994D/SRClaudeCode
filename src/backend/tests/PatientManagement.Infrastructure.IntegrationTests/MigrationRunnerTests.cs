using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Migrations;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class MigrationRunnerTests
{
    private const string CreateWidgetsSql =
        "CREATE TABLE dbo.Widgets (WidgetId int NOT NULL CONSTRAINT PK_Widgets PRIMARY KEY);";

    private const string CreateGadgetsSql =
        "CREATE TABLE dbo.Gadgets (GadgetId int NOT NULL CONSTRAINT PK_Gadgets PRIMARY KEY);";

    private const string CountWidgetsTableSql = "SELECT COUNT(*) FROM sys.tables WHERE name = N'Widgets';";
    private const string CountBrokenTableSql = "SELECT COUNT(*) FROM sys.tables WHERE name = N'Broken';";
    private const string CountHistoryRowsSql = "SELECT COUNT(*) FROM dbo.SchemaMigrations;";

    [Fact]
    public async Task ApplyAsync_EmptyDatabase_AppliesScriptAndRecordsIt()
    {
        await using var database = await TestDatabase.CreateAsync();
        var runner = CreateRunner(database);

        await runner.ApplyAsync(
            [new MigrationScript("V0001__create_widgets", CreateWidgetsSql)],
            CancellationToken.None);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountWidgetsTableSql)).Should().Be(1);
        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountHistoryRowsSql)).Should().Be(1);
    }

    [Fact]
    public async Task ApplyAsync_ScriptAlreadyApplied_SkipsItWithoutError()
    {
        await using var database = await TestDatabase.CreateAsync();
        var runner = CreateRunner(database);
        var scripts = new[] { new MigrationScript("V0001__create_widgets", CreateWidgetsSql) };
        await runner.ApplyAsync(scripts, CancellationToken.None);

        // Re-running would fail on CREATE TABLE if the script were executed again.
        await runner.ApplyAsync(scripts, CancellationToken.None);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountHistoryRowsSql)).Should().Be(1);
    }

    [Fact]
    public async Task ApplyAsync_AppliedScriptChanged_ThrowsMigrationException()
    {
        await using var database = await TestDatabase.CreateAsync();
        var runner = CreateRunner(database);
        await runner.ApplyAsync(
            [new MigrationScript("V0001__create_widgets", CreateWidgetsSql)],
            CancellationToken.None);

        var act = () => runner.ApplyAsync(
            [new MigrationScript("V0001__create_widgets", CreateGadgetsSql)],
            CancellationToken.None);

        await act.Should().ThrowAsync<MigrationException>().WithMessage("*changed after it was applied*");
    }

    [Fact]
    public async Task ApplyAsync_ScriptFails_RollsBackItsChangesAndKeepsEarlierScripts()
    {
        await using var database = await TestDatabase.CreateAsync();
        var runner = CreateRunner(database);
        var scripts = new[]
        {
            new MigrationScript("V0001__create_widgets", CreateWidgetsSql),
            new MigrationScript(
                "V0002__create_broken",
                "CREATE TABLE dbo.Broken (BrokenId int NOT NULL); THROW 50000, N'Simulated failure', 1;"),
        };

        var act = () => runner.ApplyAsync(scripts, CancellationToken.None);

        await act.Should().ThrowAsync<MigrationException>().WithMessage("*V0002__create_broken*rolled back*");
        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountHistoryRowsSql)).Should().Be(1);
        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountBrokenTableSql)).Should().Be(0);
    }

    private static MigrationRunner CreateRunner(TestDatabase database)
    {
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        return new MigrationRunner(
            new SqlConnectionFactory(options),
            options,
            NullLogger<MigrationRunner>.Instance);
    }
}
