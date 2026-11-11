namespace PatientManagement.Application.Paging;

/// <summary>
/// One page of a list, in the single shape every paged endpoint uses (CLAUDE.md 6.5): items, total count, page
/// number and page size.
/// </summary>
public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize);

/// <summary>Page limits shared by every paged endpoint (CLAUDE.md 6.5).</summary>
public static class PagingLimits
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;
}
