using FluentAssertions;
using Microsoft.Extensions.Options;
using PatientManagement.Infrastructure.IntegrationTests.Support;
using PatientManagement.Infrastructure.Persistence;
using PatientManagement.Infrastructure.Security;
using PatientManagement.Infrastructure.Seeding;

namespace PatientManagement.Infrastructure.IntegrationTests;

public sealed class SyntheticDataSeederTests
{
    private const string CountDoctorSql =
        "SELECT COUNT(*) FROM dbo.Users WHERE Username = N'dev.doctor';";

    private const string ClinicNameSql = "SELECT ClinicName FROM dbo.ClinicProfile WHERE ClinicProfileId = 1;";

    private const string DoctorPasswordHashSql =
        "SELECT PasswordHash FROM dbo.Users WHERE Username = N'dev.doctor';";

    private const string SetClinicNameSql =
        "UPDATE dbo.ClinicProfile SET ClinicName = N'Doctor Entered Name' WHERE ClinicProfileId = 1;";

    [Fact]
    public async Task SeedAsync_EmptyDatabase_CreatesOneDoctorAccount()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);

        await SeedAsync(database, new BcryptPasswordHasher().Hash("Passw0rd!"));

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountDoctorSql)).Should().Be(1);
    }

    [Fact]
    public async Task SeedAsync_RunTwice_DoesNotDuplicateTheDoctorAccount()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        var hash = new BcryptPasswordHasher().Hash("Passw0rd!");

        await SeedAsync(database, hash);
        await SeedAsync(database, hash);

        (await SqlQuery.ScalarIntAsync(database.ConnectionString, CountDoctorSql)).Should().Be(1);
    }

    [Fact]
    public async Task SeedAsync_EmptyDatabase_StoresHashThatVerifiesThePassword()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SeedAsync(database, new BcryptPasswordHasher().Hash("Passw0rd!"));

        var storedHash = await SqlQuery.ScalarStringAsync(database.ConnectionString, DoctorPasswordHashSql);

        new BcryptPasswordHasher().Verify("Passw0rd!", storedHash).Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_EmptyDatabase_FillsClinicNameWithSyntheticText()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);

        await SeedAsync(database, new BcryptPasswordHasher().Hash("Passw0rd!"));

        (await SqlQuery.ScalarStringAsync(database.ConnectionString, ClinicNameSql)).Should().Be("Synthetic Clinic");
    }

    [Fact]
    public async Task SeedAsync_ClinicNameAlreadyEntered_KeepsTheEnteredName()
    {
        await using var database = await TestDatabase.CreateAsync();
        await TestMigrations.ApplyAllAsync(database);
        await SqlQuery.ExecuteAsync(database.ConnectionString, SetClinicNameSql);

        await SeedAsync(database, new BcryptPasswordHasher().Hash("Passw0rd!"));

        (await SqlQuery.ScalarStringAsync(database.ConnectionString, ClinicNameSql)).Should().Be("Doctor Entered Name");
    }

    private static async Task SeedAsync(TestDatabase database, string passwordHash)
    {
        var options = Options.Create(new DatabaseOptions { ConnectionString = database.ConnectionString });
        var seeder = new SyntheticDataSeeder(new SqlConnectionFactory(options));

        await seeder.SeedAsync(passwordHash, CancellationToken.None);
    }
}
