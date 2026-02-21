namespace Vitarish.Modules.Elasticsearch.Application.Models;

public class SearchQueryRequest
{
    public string? SearchText { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Filters { get; init; } =
        new Dictionary<string, IReadOnlyCollection<string>>();

    public IReadOnlyCollection<SearchSortOption> Sort { get; init; } = Array.Empty<SearchSortOption>();

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;

    public string? TenantId { get; init; }
}

public record SearchSortOption(string Field, SortDirection Direction = SortDirection.Asc);

public enum SortDirection
{
    Asc,
    Desc
}
