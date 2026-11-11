using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Writes the per-user list of recently opened patients inside the current unit of work.</summary>
public sealed class SqlRecentPatientRepository(IOptions<DatabaseOptions> options) : IRecentPatientRepository
{
    // The MERGE either inserts the row or moves it to the newest time. The DELETE then removes every row beyond the
    // newest @KeepCount for this user. Both run in the caller's transaction, so the list is never left over the limit.
    private const string RecordViewSql = """
        MERGE dbo.RecentPatients AS target
        USING (SELECT @UserId AS UserId, @PatientId AS PatientId) AS source
            ON target.UserId = source.UserId AND target.PatientId = source.PatientId
        WHEN MATCHED THEN
            UPDATE SET LastViewedDate = @ViewedUtc, ModifiedDate = @ViewedUtc, ModifiedBy = @UserId
        WHEN NOT MATCHED THEN
            INSERT (UserId, PatientId, LastViewedDate, CreatedBy, ModifiedDate, ModifiedBy)
            VALUES (source.UserId, source.PatientId, @ViewedUtc, @UserId, @ViewedUtc, @UserId);

        DELETE FROM dbo.RecentPatients
        WHERE UserId = @UserId
          AND PatientId NOT IN (
              SELECT TOP (@KeepCount) PatientId
              FROM dbo.RecentPatients
              WHERE UserId = @UserId
              ORDER BY LastViewedDate DESC, PatientId DESC);
        """;

    public async Task RecordViewAsync(
        int userId,
        int patientId,
        DateTime viewedUtc,
        int keepCount,
        CancellationToken cancellationToken)
    {
        var scope = UnitOfWorkScope.Current;
        await using var command = scope.Connection.CreateCommand();
        command.CommandText = RecordViewSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Transaction = scope.Transaction;
        command.Parameters.Add(new SqlParameter("@UserId", userId));
        command.Parameters.Add(new SqlParameter("@PatientId", patientId));
        command.Parameters.Add(new SqlParameter("@ViewedUtc", viewedUtc));
        command.Parameters.Add(new SqlParameter("@KeepCount", keepCount));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
