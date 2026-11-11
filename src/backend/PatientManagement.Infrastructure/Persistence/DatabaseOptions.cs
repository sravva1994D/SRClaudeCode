namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Settings for the "Database" configuration section.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    /// <summary>Connection string without credentials. Development uses Windows authentication.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Explicit timeout for every command (CLAUDE.md section 6.3).</summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>When true, the API applies pending migrations at startup.</summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
