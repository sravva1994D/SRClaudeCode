using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auth;
using PatientManagement.Application.Auth.Handlers;
using PatientManagement.Domain.Exceptions;
using PatientManagement.Domain.Security;
using PatientManagement.Infrastructure.Auditing;
using PatientManagement.Infrastructure.Migrations;
using PatientManagement.Infrastructure.Persistence;
using PatientManagement.Infrastructure.Security;
using PatientManagement.Infrastructure.Seeding;

// Operator tool. Two commands:
//   (no arguments)                   apply migrations, then write synthetic development data
//   reset-password <username>        set a new password without the old one, and end every session (plan step 17)
// Environment variables (nothing is stored in the repository):
//   PM_CONNECTION_STRING        optional; defaults to the development LocalDB database
//   PM_SEED_DOCTOR_PASSWORD     required by the seed command: the password for the synthetic doctor account
//   PM_RESET_PASSWORD           required by reset-password: the new password
// Never point this tool at a database that holds real patients unless you mean to change its data.
const string DefaultConnectionString =
    "Server=(LocalDb)\\MSSQLLocalDB;Database=PatientManagement;Trusted_Connection=True;TrustServerCertificate=True;";

var connectionString = Environment.GetEnvironmentVariable("PM_CONNECTION_STRING") ?? DefaultConnectionString;

if (args.Length > 0 && args[0] == "reset-password")
{
    return await ResetPasswordAsync(args, connectionString);
}

return await SeedAsync(connectionString);

static async Task<int> SeedAsync(string connectionString)
{
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
        .SeedAsync(new BcryptPasswordHasher().Hash(doctorPassword), CancellationToken.None);

    Console.WriteLine($"Migrations applied and synthetic data seeded for account '{SyntheticDataSeeder.DoctorUsername}'.");
    return 0;
}

static async Task<int> ResetPasswordAsync(string[] args, string connectionString)
{
    if (args.Length != 2 || string.IsNullOrWhiteSpace(args[1]))
    {
        Console.Error.WriteLine("Usage: reset-password <username>   (the new password comes from PM_RESET_PASSWORD)");
        return 1;
    }

    var newPassword = Environment.GetEnvironmentVariable("PM_RESET_PASSWORD");
    if (string.IsNullOrEmpty(newPassword))
    {
        Console.Error.WriteLine("Set PM_RESET_PASSWORD to the new password.");
        return 1;
    }

    var options = Options.Create(new DatabaseOptions { ConnectionString = connectionString });
    var connectionFactory = new SqlConnectionFactory(options);
    var clock = new SystemClock();
    var audit = new SqlAuditLogWriter(clock, options);
    var handler = new ResetPasswordCommandHandler(
        new SqlUnitOfWork(connectionFactory),
        new SqlUserAccountRepository(audit, clock, options),
        new SqlRefreshTokenRepository(audit, clock, options),
        new BcryptPasswordHasher(),
        clock);

    try
    {
        await handler.HandleAsync(new ResetPasswordCommand(args[1], newPassword), CancellationToken.None);
    }
    catch (DomainException exception)
    {
        Console.Error.WriteLine($"Password not changed: {exception.Message}");
        return 1;
    }

    Console.WriteLine($"Password changed for '{args[1]}'. All of its sessions have ended and the change is in the audit log.");
    return 0;
}

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
