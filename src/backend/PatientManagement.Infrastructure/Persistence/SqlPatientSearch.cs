using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using PatientManagement.Application.Interfaces;
using PatientManagement.Application.Paging;
using PatientManagement.Application.Patients;

namespace PatientManagement.Infrastructure.Persistence;

/// <summary>Reads one page of active patients and the total count in one round trip.</summary>
public sealed class SqlPatientSearch(
    ISqlConnectionFactory connectionFactory,
    IOptions<DatabaseOptions> options) : IPatientSearch
{
    // Both filters are optional: a null parameter disables its predicate. The count and the page share one predicate,
    // so the total always matches the rows that can be paged through.
    private const string FilterSql = """
        WHERE IsActive = 1
          AND (@NamePattern IS NULL OR FullName LIKE @NamePattern ESCAPE '\')
          AND (@PhonePattern IS NULL OR PhoneNumber LIKE @PhonePattern)
        """;

    private static readonly string SearchSql =
        $"""
        SELECT COUNT(*) FROM dbo.Patients
        {FilterSql};

        SELECT PatientId, FullName, DateOfBirth, GenderId, PhoneNumber
        FROM dbo.Patients
        {FilterSql}
        ORDER BY FullName, PatientId
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

    public async Task<PagedResponse<PatientListItem>> SearchActiveAsync(
        string? nameTerm,
        string? phoneDigits,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = SearchSql;
        command.CommandTimeout = options.Value.CommandTimeoutSeconds;
        command.Parameters.Add(Nullable("@NamePattern", nameTerm is null ? null : ContainsPattern(nameTerm, escape: true)));
        command.Parameters.Add(Nullable("@PhonePattern", phoneDigits is null ? null : ContainsPattern(phoneDigits, escape: false)));
        command.Parameters.Add(new SqlParameter("@Offset", (pageNumber - 1) * pageSize));
        command.Parameters.Add(new SqlParameter("@PageSize", pageSize));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        var totalCount = reader.GetInt32(0);

        await reader.NextResultAsync(cancellationToken);
        var items = new List<PatientListItem>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new PatientListItem(
                reader.GetInt32(0),
                reader.GetString(1),
                DateOnly.FromDateTime(reader.GetDateTime(2)),
                reader.GetInt32(3),
                reader.IsDBNull(4) ? null : reader.GetString(4)));
        }

        return new PagedResponse<PatientListItem>(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Wraps the text in % signs. Name text escapes the LIKE wildcards, so a name containing % or _ matches literally.
    /// </summary>
    private static string ContainsPattern(string text, bool escape) =>
        "%" + (escape ? EscapeLike(text) : text) + "%";

    private static string EscapeLike(string text) => text
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal)
        .Replace("[", "\\[", StringComparison.Ordinal);

    private static SqlParameter Nullable(string name, string? value) => new(name, (object?)value ?? DBNull.Value);
}
