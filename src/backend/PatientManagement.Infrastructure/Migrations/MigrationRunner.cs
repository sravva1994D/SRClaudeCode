using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.Migrations;

/// <summary>
/// Applies pending scripts in version order. Each script and its history row commit together, and a
/// session-level application lock stops two application instances from migrating at the same time.
/// </summary>
public sealed partial class MigrationRunner(
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options,
    ILogger<MigrationRunner> logger)
{
    private const string LockResourceName = "PatientManagement.SchemaMigrations";
    private const int LockTimeoutMilliseconds = 60_000;

    // Created by the runner, not by a script, because the runner needs this table to record every script.
    private const string CreateHistoryTableSql = """
        IF OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NULL
        BEGIN
            CREATE TABLE dbo.SchemaMigrations (
                ScriptName nvarchar(200) NOT NULL CONSTRAINT PK_SchemaMigrations PRIMARY KEY,
                Checksum char(64) NOT NULL,
                AppliedDate datetime2(3) NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedDate DEFAULT SYSUTCDATETIME()
            );
        END
        """;

    private const string AcquireLockSql = """
        DECLARE @result int;
        EXEC @result = sp_getapplock @Resource = @Resource, @LockMode = N'Exclusive', @LockOwner = N'Session', @LockTimeout = @LockTimeout;
        SELECT @result;
        """;

    private const string ReleaseLockSql = "EXEC sp_releaseapplock @Resource = @Resource, @LockOwner = N'Session';";
    private const string ReadAppliedSql = "SELECT ScriptName, Checksum FROM dbo.SchemaMigrations;";
    private const string InsertHistorySql =
        "INSERT INTO dbo.SchemaMigrations (ScriptName, Checksum) VALUES (@ScriptName, @Checksum);";

    [LoggerMessage(Level = LogLevel.Information, Message = "Migrations checked: {AppliedCount} applied in this run, {KnownCount} known to this build")]
    private static partial void LogMigrationsChecked(ILogger logger, int appliedCount, int knownCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Applied migration {MigrationName}")]
    private static partial void LogMigrationApplied(ILogger logger, string migrationName);

    public async Task ApplyAsync(IReadOnlyList<MigrationScript> scripts, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await AcquireLockAsync(connection, cancellationToken);
        try
        {
            await ExecuteNonQueryAsync(connection, CreateHistoryTableSql, cancellationToken);
            var applied = await ReadAppliedChecksumsAsync(connection, cancellationToken);
            VerifyAppliedScripts(scripts, applied);

            var pending = scripts.Where(script => !applied.ContainsKey(script.Name)).ToList();
            foreach (var script in pending)
            {
                await ApplyScriptAsync(connection, script, cancellationToken);
            }

            LogMigrationsChecked(logger, pending.Count, scripts.Count);
        }
        finally
        {
            await ReleaseLockAsync(connection);
        }
    }

    private async Task AcquireLockAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        // The command timeout must outlast the lock wait, or the command fails before the lock does.
        await using var command = CreateCommand(connection, AcquireLockSql, timeoutSeconds: LockTimeoutMilliseconds / 1000 + 5);
        command.Parameters.Add(new SqlParameter("@Resource", LockResourceName));
        command.Parameters.Add(new SqlParameter("@LockTimeout", LockTimeoutMilliseconds));

        var result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (result < 0)
        {
            throw new MigrationException($"Could not acquire the migration lock (sp_getapplock returned {result}).");
        }
    }

    private async Task ReleaseLockAsync(DbConnection connection)
    {
        // Runs even when the caller was cancelled, so the lock never outlives this connection's work.
        await using var command = CreateCommand(connection, ReleaseLockSql);
        command.Parameters.Add(new SqlParameter("@Resource", LockResourceName));
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }

    private DbCommand CreateCommand(
        DbConnection connection,
        string sql,
        DbTransaction? transaction = null,
        int? timeoutSeconds = null)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = timeoutSeconds ?? options.Value.CommandTimeoutSeconds;
        command.Transaction = transaction;
        return command;
    }

    private async Task ExecuteNonQueryAsync(DbConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<Dictionary<string, string>> ReadAppliedChecksumsAsync(
        DbConnection connection,
        CancellationToken cancellationToken)
    {
        var applied = new Dictionary<string, string>(StringComparer.Ordinal);
        await using var command = connection.CreateCommand();
        command.CommandText = ReadAppliedSql;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            applied[reader.GetString(0)] = reader.GetString(1);
        }

        return applied;
    }

    private static void VerifyAppliedScripts(
        IReadOnlyList<MigrationScript> scripts,
        IReadOnlyDictionary<string, string> applied)
    {
        var knownNames = scripts.Select(script => script.Name).ToHashSet(StringComparer.Ordinal);
        var unknownNames = applied.Keys.Where(name => !knownNames.Contains(name)).Order(StringComparer.Ordinal).ToList();
        if (unknownNames.Count > 0)
        {
            throw new MigrationException(
                $"The database has migrations this build does not contain: {string.Join(", ", unknownNames)}.");
        }

        foreach (var script in scripts)
        {
            if (applied.TryGetValue(script.Name, out var appliedChecksum) && appliedChecksum != script.Checksum)
            {
                throw new MigrationException(
                    $"Migration {script.Name} changed after it was applied. Add a new migration instead.");
            }
        }
    }

    private async Task ApplyScriptAsync(DbConnection connection, MigrationScript script, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await using (var scriptCommand = CreateCommand(connection, script.Sql, transaction))
            {
                await scriptCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var historyCommand = CreateCommand(connection, InsertHistorySql, transaction))
            {
                historyCommand.Parameters.Add(new SqlParameter("@ScriptName", script.Name));
                historyCommand.Parameters.Add(new SqlParameter("@Checksum", script.Checksum));
                await historyCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            LogMigrationApplied(logger, script.Name);
        }
        catch (Exception exception)
        {
            // The script's error text is not logged or copied into the message: it can contain patient data.
            await transaction.RollbackAsync(CancellationToken.None);
            throw new MigrationException($"Migration {script.Name} failed and was rolled back.", exception);
        }
    }
}
