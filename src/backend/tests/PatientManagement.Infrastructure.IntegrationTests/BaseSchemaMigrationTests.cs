using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Migrations;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class BaseSchemaMigrationTests
{
    private const string CountHistoryRowsSql = "SELECT COUNT(*) FROM dbo.SchemaMigrations;";

    private const string CountBaseTablesSql = """
        SELECT COUNT(*) FROM sys.tables
        WHERE name IN (N'Users', N'Genders', N'BloodGroups', N'AppointmentStatuses', N'VitalRanges', N'ClinicProfile');
        """;

    // Counts user tables that lack any of the six audit columns from CLAUDE.md 6.4.
    private const string CountTablesWithoutAuditColumnsSql = """
        SELECT COUNT(*) FROM sys.tables AS t
        WHERE t.name <> N'SchemaMigrations'
          AND (SELECT COUNT(*) FROM sys.columns AS c
               WHERE c.object_id = t.object_id
                 AND c.name IN (N'IsActive', N'CreatedDate', N'CreatedBy', N'ModifiedDate', N'ModifiedBy', N'RowVersion')) < 6;
        """;

    private const string CountGendersSql = "SELECT COUNT(*) FROM dbo.Genders;";
    private const string CountBloodGroupsSql = "SELECT COUNT(*) FROM dbo.BloodGroups;";
    private const string CountAppointmentStatusesSql = "SELECT COUNT(*) FROM dbo.AppointmentStatuses;";
    private const string CountClinicProfileRowsSql = "SELECT COUNT(*) FROM dbo.ClinicProfile;";

    [Fact]
    public async Task ApplyAsync_EmbeddedScripts_RecordsEveryBaseScript()
    {
        await using var database = await TestDatabase.CreateAsync();
        await MigrateAsync(database);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountHistoryRowsSql)).Should().Be(3);
    }

    [Fact]
    public async Task ApplyAsync_EmbeddedScripts_CreatesSixBaseTables()
    {
        await using var database = await TestDatabase.CreateAsync();
        await MigrateAsync(database);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountBaseTablesSql)).Should().Be(6);
    }

    [Fact]
    public async Task ApplyAsync_EmbeddedScripts_EveryTableHasAuditColumns()
    {
        await using var database = await TestDatabase.CreateAsync();
        await MigrateAsync(database);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountTablesWithoutAuditColumnsSql)).Should().Be(0);
    }

    [Fact]
    public async Task ApplyAsync_EmbeddedScripts_SeedsThreeGenders()
    {
        await using var database = await TestDatabase.CreateAsync();
        await MigrateAsync(database);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountGendersSql)).Should().Be(3);
    }

    [Fact]
    public async Task ApplyAsync_EmbeddedScripts_SeedsNineBloodGroups()
    {
        await using var database = await TestDatabase.CreateAsync();
        await MigrateAsync(database);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountBloodGroupsSql)).Should().Be(9);
    }

    [Fact]
    public async Task ApplyAsync_EmbeddedScripts_SeedsFourAppointmentStatuses()
    {
        await using var database = await TestDatabase.CreateAsync();
        await MigrateAsync(database);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountAppointmentStatusesSql)).Should().Be(4);
    }

    [Fact]
    public async Task ApplyAsync_EmbeddedScripts_CreatesOneClinicProfileRow()
    {
        await using var database = await TestDatabase.CreateAsync();
        await MigrateAsync(database);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountClinicProfileRowsSql)).Should().Be(1);
    }

    [Fact]
    public async Task Insert_DuplicateUsername_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        await MigrateAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, InsertUserSql("doctor"));

        var act = () => SqlQuery.ExecuteAsync(database.ConnectionString, InsertUserSql("doctor"));

        await act.Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public async Task Insert_NegativeFailedLoginCount_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        await MigrateAsync(database);

        var act = () => SqlQuery.ExecuteAsync(
            database.ConnectionString,
            "INSERT INTO dbo.Users (Username, PasswordHash, FailedLoginCount) VALUES (N'doctor', N'hash', -1);");

        await act.Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public async Task Insert_CreatedByUnknownUser_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        await MigrateAsync(database);

        var act = () => SqlQuery.ExecuteAsync(
            database.ConnectionString,
            "INSERT INTO dbo.Genders (GenderId, Name, CreatedBy) VALUES (10, N'Test', 999);");

        await act.Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public async Task Insert_SecondClinicProfileRow_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        await MigrateAsync(database);

        var act = () => SqlQuery.ExecuteAsync(
            database.ConnectionString,
            "INSERT INTO dbo.ClinicProfile (ClinicProfileId) VALUES (2);");

        await act.Should().ThrowAsync<SqlException>();
    }

    [Fact]
    public async Task Insert_PlausibleRangeNotContainingNormalRange_IsRejected()
    {
        await using var database = await TestDatabase.CreateAsync();
        await MigrateAsync(database);

        var act = () => SqlQuery.ExecuteAsync(
            database.ConnectionString,
            """
            INSERT INTO dbo.VitalRanges (VitalRangeId, VitalSign, Unit, NormalMinimum, NormalMaximum, PlausibleMinimum, PlausibleMaximum)
            VALUES (1, N'Pulse', N'BPM', 40, 200, 50, 220);
            """);

        await act.Should().ThrowAsync<SqlException>();
    }

    private static string InsertUserSql(string username) =>
        $"INSERT INTO dbo.Users (Username, PasswordHash) VALUES (N'{username}', N'hash');";

    private static async Task MigrateAsync(TestDatabase database)
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
