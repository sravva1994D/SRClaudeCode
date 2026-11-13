using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Paging;
using PatientManagement.Application.Visits;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>
/// Reads a patient's visit history in two result sets: the total and the requested page. The date bounds are inclusive UTC
/// days; the upper bound is compared as "before the next day" so the whole last day is included.
/// </summary>
public sealed class SqlVisitHistoryRepository(
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IVisitHistoryRepository
{
    private const string Filter = """
        PatientId = @PatientId AND IsActive = 1
        AND (@FromUtc IS NULL OR CreatedDate >= @FromUtc)
        AND (@ToExclusiveUtc IS NULL OR CreatedDate < @ToExclusiveUtc)
        """;

    // Newest first; VisitId breaks ties so paging is stable. The index IX_Visits_PatientId_CreatedDate covers the filter.
    private static readonly string HistorySql = $"""
        SELECT COUNT(*)
        FROM dbo.Visits
        WHERE {Filter};

        SELECT VisitId, CreatedDate, VisitStatusId, ModifiedDate, AppointmentId
        FROM dbo.Visits
        WHERE {Filter}
        ORDER BY CreatedDate DESC, VisitId DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    public async Task<PagedResponse<PatientVisitHistoryItem>> ListForPatientAsync(
        PatientVisitHistoryQuery query,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = HistorySql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Parameters.Add(new SqlParameter("@PatientId", query.PatientId));
        command.Parameters.Add(new SqlParameter("@FromUtc", DateBound(query.From)));
        command.Parameters.Add(new SqlParameter("@ToExclusiveUtc", DateBound(query.To?.AddDays(1))));
        command.Parameters.Add(new SqlParameter("@Offset", (query.PageNumber - 1) * query.PageSize));
        command.Parameters.Add(new SqlParameter("@PageSize", query.PageSize));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        var totalCount = reader.GetInt32(0);

        await reader.NextResultAsync(cancellationToken);
        var items = new List<PatientVisitHistoryItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PatientVisitHistoryItem(
                reader.GetInt32(0),
                DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc),
                reader.GetInt32(2),
                DateTime.SpecifyKind(reader.GetDateTime(3), DateTimeKind.Utc),
                reader.IsDBNull(4) ? null : reader.GetInt32(4)));
        }

        return new PagedResponse<PatientVisitHistoryItem>(items, totalCount, query.PageNumber, query.PageSize);
    }

    private static object DateBound(DateOnly? day) =>
        day is null ? DBNull.Value : day.Value.ToDateTime(TimeOnly.MinValue);
}
