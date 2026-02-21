namespace Vitarish.Modules.Elasticsearch.Application.Models;

public class SearchQueryResult
{
    public IReadOnlyCollection<SearchHit> Hits { get; init; } = Array.Empty<SearchHit>();

    public long Total { get; init; }
}

public class SearchHit
{
    public required string Id { get; init; }

    public double Score { get; init; }

    public IReadOnlyDictionary<string, IReadOnlyCollection<string>> Highlights { get; init; } =
        new Dictionary<string, IReadOnlyCollection<string>>();
}
