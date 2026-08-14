using System.Text.Json;
using System.Text.Json.Serialization;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Search.Agents;

public enum SearchAgentIndexScope
{
    AllRegistered = 0,
    AllowList = 1
}

public sealed class SearchAgentOptions
{
    public SearchAgentIndexScope IndexScope { get; set; } = SearchAgentIndexScope.AllRegistered;
    public string[] AllowedIndexes { get; set; } = [];

    public IReadOnlySet<string> GetNormalizedAllowedIndexes()
    {
        IReadOnlySet<string> indexes = (AllowedIndexes ?? [])
            .Where(index => !string.IsNullOrWhiteSpace(index))
            .Select(index => index.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (IndexScope == SearchAgentIndexScope.AllowList && indexes.Count == 0)
            throw new InvalidOperationException("Search agent allowlist scope requires at least one index.");

        return indexes;
    }
}

public interface ISearchAgentTool : IIntelligenceAgentTool;

public sealed class SearchAgentTool : ISearchAgentTool
{
    private const string InvalidArgumentsCode = "invalid_search_arguments";
    private const string IndexNotAllowedCode = "search_index_not_allowed";
    private const string IndexNotRegisteredCode = "search_index_not_registered";
    private static readonly HashSet<string> ForbiddenFilterKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "tenantId", "patientId", "userId", "authenticatedUserId", "practiceId", "authorization", "credential", "accessToken"
    };

    private readonly ISearchService _searchService;
    private readonly SearchAgentOptions _options;
    private readonly IReadOnlySet<string> allowedIndexes;

    public SearchAgentTool(ISearchService searchService, SearchAgentOptions options)
    {
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        allowedIndexes = _options.GetNormalizedAllowedIndexes();
    }

    public AgentToolDefinition Definition { get; } = new(
        "search",
        "Search tenant-scoped registered indexes and return a bounded list of safe document summaries.",
        """
        {"type":"object","properties":{"query":{"type":"string","minLength":1,"maxLength":500},"registeredIndex":{"type":"string","minLength":1,"maxLength":200},"filters":{"type":"object","additionalProperties":false,"properties":{"type":{"type":"string","maxLength":200},"corpusId":{"type":"string","maxLength":200},"metadataFilters":{"type":"object","maxProperties":20,"additionalProperties":{"type":"string","maxLength":200}}}},"limit":{"type":"integer","minimum":1,"maximum":100},"retrievalMode":{"type":"string","enum":["strict","balanced","broad"]}},"required":["query"],"additionalProperties":false}
        """);

    public async Task<AgentToolResult> ExecuteAsync(
        AgentToolCall call,
        AgentExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SearchAgentArguments arguments;
        try
        {
            arguments = JsonSerializer.Deserialize(
                call.ArgumentsJson,
                SearchAgentJsonSerializerContext.Default.SearchAgentArguments)
                ?? throw new JsonException("Search arguments are required.");
            Validate(arguments);
        }
        catch (JsonException)
        {
            return Error(call, InvalidArgumentsCode, "Provide a valid tenant-scoped search query and supported filters.");
        }
        catch (FormatException)
        {
            return Error(call, InvalidArgumentsCode, "The retrieval mode must be strict, balanced, or broad.");
        }

        SearchRetrievalMode retrievalMode = ParseRetrievalMode(arguments.RetrievalMode);
        int limit = SearchOptions.NormalizeLimit(arguments.Limit ?? SearchOptions.DefaultLimit);
        SearchOptions searchOptions = new()
        {
            Limit = limit,
            Type = arguments.Filters?.Type,
            CorpusId = arguments.Filters?.CorpusId,
            MetadataFilters = arguments.Filters?.MetadataFilters ?? new Dictionary<string, string>(),
            RetrievalMode = retrievalMode
        };

        IReadOnlyList<string?> targetIndexes;
        try
        {
            targetIndexes = ResolveTargetIndexes(arguments.RegisteredIndex);
        }
        catch (SearchAgentArgumentException exception)
        {
            return Error(call, exception.Code, exception.Message);
        }
        if (targetIndexes.Count == 0)
        {
            return Success(call, arguments.Query!, [], 0);
        }

        List<SearchDocument> items = [];
        long totalCount = 0;
        foreach (string? index in targetIndexes)
        {
            SearchResult result = await _searchService.SearchAsync(
                index,
                arguments.Query!,
                searchOptions,
                cancellationToken);
            ValidateResult(result, arguments.Query!);
            totalCount = checked(totalCount + result.TotalCount);
            items.AddRange(result.Items);
        }

        return Success(call, arguments.Query!, items.Take(limit).ToArray(), totalCount);
    }

    private IReadOnlyList<string?> ResolveTargetIndexes(string? requestedIndex)
    {
        IReadOnlySet<string> registeredIndexes = _searchService.GetIndexPolicyReports()
            .Select(report => report.IndexName)
            .Where(index => !string.IsNullOrWhiteSpace(index))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(requestedIndex))
        {
            string normalizedIndex = requestedIndex.Trim();
            if (!registeredIndexes.Contains(normalizedIndex))
                throw new SearchAgentArgumentException(IndexNotRegisteredCode, "The requested index is not registered.");
            if (_options.IndexScope == SearchAgentIndexScope.AllowList && !allowedIndexes.Contains(normalizedIndex))
                throw new SearchAgentArgumentException(IndexNotAllowedCode, "The requested index is outside the Search agent scope.");
            return [normalizedIndex];
        }

        if (_options.IndexScope == SearchAgentIndexScope.AllRegistered)
            return [null];

        return allowedIndexes
            .Where(registeredIndexes.Contains)
            .OrderBy(index => index, StringComparer.Ordinal)
            .Cast<string?>()
            .ToArray();
    }

    private static void Validate(SearchAgentArguments arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments.Query) || arguments.Query.Length > 500)
            throw new JsonException("A bounded search query is required.");
        if (arguments.RegisteredIndex is { Length: > 200 })
            throw new JsonException("The registered index is too long.");
        if (arguments.Filters?.Type is { Length: > 200 } || arguments.Filters?.CorpusId is { Length: > 200 })
            throw new JsonException("Search filters are too long.");
        if (arguments.Filters?.MetadataFilters is { Count: > 20 })
            throw new JsonException("Too many metadata filters were supplied.");
        if (arguments.Filters?.MetadataFilters is not null && arguments.Filters.MetadataFilters.Any(filter =>
                string.IsNullOrWhiteSpace(filter.Key) || filter.Key.Length > 200 || filter.Value is null || filter.Value.Length > 200 ||
                ForbiddenFilterKeys.Contains(filter.Key)))
            throw new JsonException("Search metadata filters must be bounded key/value pairs.");

        _ = ParseRetrievalMode(arguments.RetrievalMode);
    }

    private static SearchRetrievalMode ParseRetrievalMode(string? value) => value?.ToLowerInvariant() switch
    {
        null or "balanced" => SearchRetrievalMode.Balanced,
        "strict" => SearchRetrievalMode.Strict,
        "broad" => SearchRetrievalMode.Broad,
        _ => throw new FormatException("The retrieval mode is invalid.")
    };

    private static void ValidateResult(SearchResult? result, string query)
    {
        if (result is null || result.Items is null || result.TotalCount < 0 || !string.Equals(result.Query, query, StringComparison.Ordinal))
            throw new InvalidOperationException("The Search service returned an invalid result envelope.");

        if (result.Items.Any(item => item is null ||
            string.IsNullOrWhiteSpace(item.Id) ||
            string.IsNullOrWhiteSpace(item.Title) ||
            string.IsNullOrWhiteSpace(item.Type)))
            throw new InvalidOperationException("The Search service returned an invalid document projection.");
    }

    private static AgentToolResult Success(
        AgentToolCall call,
        string query,
        IReadOnlyList<SearchDocument> items,
        long totalCount) => new(
        call.Id,
        JsonSerializer.Serialize(
            new SearchAgentToolResponse(
                items.Select(item => new SearchAgentToolItem(
                    item.Id,
                    item.Title,
                    item.Type,
                    item.IndexName,
                    item.Score)).ToArray(),
                totalCount,
                query),
            SearchAgentJsonSerializerContext.Default.SearchAgentToolResponse));

    private static AgentToolResult Error(AgentToolCall call, string code, string message) => new(
        call.Id,
        JsonSerializer.Serialize(new SearchAgentToolError(code, message), SearchAgentJsonSerializerContext.Default.SearchAgentToolError),
        IsError: true);

    private sealed class SearchAgentArgumentException(string code, string message) : Exception(message)
    {
        public string Code { get; } = code;
    }
}

internal sealed record SearchAgentArguments
{
    public string? Query { get; init; }
    public string? RegisteredIndex { get; init; }
    public SearchAgentFilters? Filters { get; init; }
    public int? Limit { get; init; }
    public string? RetrievalMode { get; init; }
}

internal sealed record SearchAgentFilters
{
    public string? Type { get; init; }
    public string? CorpusId { get; init; }
    public Dictionary<string, string>? MetadataFilters { get; init; }
}

internal sealed record SearchAgentToolResponse(
    IReadOnlyList<SearchAgentToolItem> Items,
    long TotalCount,
    string Query);

internal sealed record SearchAgentToolItem(
    string Id,
    string Title,
    string Type,
    string? IndexName,
    double? Score);

internal sealed record SearchAgentToolError(string Code, string Message);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)]
[JsonSerializable(typeof(SearchAgentArguments))]
[JsonSerializable(typeof(SearchAgentToolResponse))]
[JsonSerializable(typeof(SearchAgentToolError))]
internal sealed partial class SearchAgentJsonSerializerContext : JsonSerializerContext;
