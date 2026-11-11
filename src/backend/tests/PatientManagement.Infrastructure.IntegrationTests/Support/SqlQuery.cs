using Microsoft.Data.SqlClient;

namespace PatientManagement.Infrastructure.IntegrationTests.Support;

/// <summary>Reads single values from a test database for assertions.</summary>
public static class SqlQuery
{
    public static async Task<int> ScalarIntAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        // Callers pass literal test queries only; the helper is never given input from elsewhere.
#pragma warning disable CA2100
        command.CommandText = sql;
#pragma warning restore CA2100
        return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    public static async Task<string> ScalarStringAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        // Callers pass literal test queries only; the helper is never given input from elsewhere.
#pragma warning disable CA2100
        command.CommandText = sql;
#pragma warning restore CA2100
        return Convert.ToString(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture)
            ?? string.Empty;
    }

    public static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();

        // Callers pass literal test statements only; the helper is never given input from elsewhere.
#pragma warning disable CA2100
        command.CommandText = sql;
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }
}
