using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Auditing;
using PatientManagement.Application.Interfaces;
using PatientManagement.Infrastructure.Persistence;

namespace PatientManagement.Infrastructure.Auditing;

/// <summary>
/// Appends audit rows inside the current unit of work. Values are serialized as JSON; the values themselves are
/// never logged, because they contain patient data.
/// </summary>
public sealed class SqlAuditLogWriter(IClock clock, IOptions<DatabaseOptions> options) : IAuditLogWriter
{
    private const string InsertSql = """
        INSERT INTO dbo.AuditLogs (TableName, RecordId, Action, OldValues, NewValues, ChangedDate, UserId)
        VALUES (@TableName, @RecordId, @Action, @OldValues, @NewValues, @ChangedDate, @UserId);
        """;

    public async Task WriteAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        var snapshot = AuditChanges.Resolve(entry);

        await using var command = scope.Connection.CreateCommand();
        command.CommandText = InsertSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@TableName", entry.TableName));
        command.Parameters.Add(new SqlParameter("@RecordId", entry.RecordId));
        command.Parameters.Add(new SqlParameter("@Action", entry.Action.ToString()));
        command.Parameters.Add(new SqlParameter("@OldValues", ToJsonOrNull(snapshot.OldValues)));
        command.Parameters.Add(new SqlParameter("@NewValues", ToJsonOrNull(snapshot.NewValues)));
        command.Parameters.Add(new SqlParameter("@ChangedDate", clock.UtcNow));
        command.Parameters.Add(new SqlParameter("@UserId", (object?)entry.UserId ?? DBNull.Value));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static object ToJsonOrNull(IReadOnlyDictionary<string, object?>? values) =>
        values is null ? DBNull.Value : JsonSerializer.Serialize(values);
}
