using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PatientManagement.Domain.Security;
using PatientManagement.Infrastructure.Migrations;
using PatientManagement.Infrastructure.Persistence;
using PatientManagement.Infrastructure.Security;
using PatientManagement.Infrastructure.Seeding;

// Development tool: applies migrations, then writes synthetic data. Never point it at a database with real patients.
// Environment variables (nothing is stored in the repository):
//   PM_CONNECTION_STRING        optional; defaults to the development LocalDB database
//   PM_SEED_DOCTOR_PASSWORD     required; the password for the synthetic doctor account
const string DefaultConnectionString =
    "Server=(LocalDb)\\MSSQLLocalDB;Database=PatientManagement;Trusted_Connection=True;TrustServerCertificate=True;";

var connectionString = Environment.GetEnvironmentVariable("PM_CONNECTION_STRING") ?? DefaultConnectionString;
var doctorPassword = Environment.GetEnvironmentVariable("PM_SEED_DOCTOR_PASSWORD");

if (string.IsNullOrEmpty(doctorPassword))
{
    Console.Error.WriteLine("Set PM_SEED_DOCTOR_PASSWORD to the password for the synthetic doctor account.");
    return 1;
}

if (!PasswordPolicy.IsSatisfiedBy(doctorPassword))
{
    Console.Error.WriteLine("The seed password does not meet the password policy (8+ characters, upper, lower, digit, symbol).");
    return 1;
}

await CreateDatabaseIfMissingAsync(connectionString);

var options = Options.Create(new DatabaseOptions { ConnectionString = connectionString });
var connectionFactory = new SqlConnectionFactory(options);

var scripts = EmbeddedMigrationScripts.Load(typeof(MigrationRunner).Assembly);
await new MigrationRunner(connectionFactory, options, NullLogger<MigrationRunner>.Instance)
    .ApplyAsync(scripts, CancellationToken.None);

await new SyntheticDataSeeder(connectionFactory)
    .SeedAsync(PasswordHasher.Hash(doctorPassword), CancellationToken.None);

Console.WriteLine($"Migrations applied and synthetic data seeded for account '{SyntheticDataSeeder.DoctorUsername}'.");
return 0;

// First run on a developer machine: the database named in the connection string may not exist yet.
static async Task CreateDatabaseIfMissingAsync(string connectionString)
{
    var databaseName = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
    if (string.IsNullOrWhiteSpace(databaseName))
    {
        throw new InvalidOperationException("The connection string must name a database.");
    }

    var masterBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
    await using var connection = new SqlConnection(masterBuilder.ConnectionString);
    await connection.OpenAsync();

    // The name comes from the developer's own connection string, not from user input; brackets are escaped.
    var quotedName = "[" + databaseName.Replace("]", "]]", StringComparison.Ordinal) + "]";
    await using var command = connection.CreateCommand();
    command.CommandText = $"IF DB_ID(@Name) IS NULL CREATE DATABASE {quotedName};";
    command.Parameters.Add(new SqlParameter("@Name", databaseName));
    await command.ExecuteNonQueryAsync();
}
