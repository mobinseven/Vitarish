using System.Text.Json;
using FluentAssertions;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Search.Agents;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Search.Tests.Agents;

public sealed class SearchAgentToolTests
{
    [Fact]
    public async Task Search_arguments_are_tenant_safe_and_projection_excludes_sensitive_document_fields()
    {
        FakeSearchService service = new();
        SearchAgentTool tool = CreateTool(service);

        AgentToolResult result = await tool.ExecuteAsync(Call("""
            {"query":"appointment","filters":{"type":"event"}}
            """), new("run-1"));

        result.IsError.Should().BeFalse();
        service.LastOptions!.TenantId.Should().BeNull();
        tool.Definition.ParametersJsonSchema.Should().NotContain("tenantId");
        tool.Definition.ParametersJsonSchema.Should().NotContain("patientId");

        using JsonDocument output = JsonDocument.Parse(result.Output);
        JsonElement item = output.RootElement.GetProperty("items")[0];
        item.EnumerateObject().Select(property => property.Name).Should()
            .NotContain(new[] { "tenantId", "body", "semanticText", "metadata" });
        item.GetProperty("title").GetString().Should().Be("Appointment");
    }

    [Fact]
    public async Task Search_forwards_filters_and_bounds_the_limit()
    {
        FakeSearchService service = new();
        SearchAgentTool tool = CreateTool(service);

        _ = await tool.ExecuteAsync(Call("""
            {"query":"appointment","filters":{"type":"event","corpusId":"clinic","metadataFilters":{"status":"open"}},"limit":999,"retrievalMode":"strict"}
            """), new("run-1"));

        service.LastIndex.Should().BeNull();
        service.LastOptions.Should().BeEquivalentTo(new SearchOptions
        {
            Limit = SearchOptions.MaxLimit,
            Type = "event",
            CorpusId = "clinic",
            MetadataFilters = new Dictionary<string, string> { ["status"] = "open" },
            RetrievalMode = SearchRetrievalMode.Strict
        });
    }

    [Fact]
    public async Task Search_uses_the_shared_default_limit_when_limit_is_omitted()
    {
        FakeSearchService service = new();
        SearchAgentTool tool = CreateTool(service);

        _ = await tool.ExecuteAsync(Call("""{"query":"appointment"}"""), new("run-1"));

        service.LastOptions!.Limit.Should().Be(SearchOptions.DefaultLimit);
    }

    [Fact]
    public async Task Allowlist_scope_queries_only_allowed_registered_indexes()
    {
        FakeSearchService service = new();
        SearchAgentTool tool = CreateTool(service, new SearchAgentOptions
        {
            IndexScope = SearchAgentIndexScope.AllowList,
            AllowedIndexes = ["events"]
        });

        _ = await tool.ExecuteAsync(Call("""{"query":"visit"}"""), new("run-1"));

        service.Calls.Select(call => call.IndexName).Should().Equal("events");
    }

    [Fact]
    public async Task All_registered_scope_delegates_unrestricted_index_selection_to_search_service()
    {
        FakeSearchService service = new();
        SearchAgentTool tool = CreateTool(service);

        _ = await tool.ExecuteAsync(Call("""{"query":"visit"}"""), new("run-1"));

        service.Calls.Select(call => call.IndexName).Should().Equal((string?)null);
    }

    [Fact]
    public async Task Disallowed_index_and_invalid_arguments_return_safe_tool_errors()
    {
        FakeSearchService service = new();
        SearchAgentTool tool = CreateTool(service, new SearchAgentOptions
        {
            IndexScope = SearchAgentIndexScope.AllowList,
            AllowedIndexes = ["events"]
        });

        AgentToolResult disallowed = await tool.ExecuteAsync(Call("""{"query":"visit","registeredIndex":"notes"}"""), new("run-1"));
        AgentToolResult invalid = await tool.ExecuteAsync(Call("""{"query":"visit","retrievalMode":"unknown"}"""), new("run-1"));

        disallowed.IsError.Should().BeTrue();
        invalid.IsError.Should().BeTrue();
        service.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Unregistered_index_returns_a_safe_tool_error()
    {
        FakeSearchService service = new();
        SearchAgentTool tool = CreateTool(service);

        AgentToolResult result = await tool.ExecuteAsync(
            Call("""{"query":"visit","registeredIndex":"missing"}"""),
            new("run-1"));

        result.IsError.Should().BeTrue();
        service.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Sensitive_scope_filter_names_are_rejected()
    {
        FakeSearchService service = new();
        SearchAgentTool tool = CreateTool(service);

        AgentToolResult result = await tool.ExecuteAsync(Call("""
            {"query":"visit","filters":{"metadataFilters":{"tenantId":"other-tenant"}}}
            """), new("run-1"));

        result.IsError.Should().BeTrue();
        service.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Cancellation_and_dependency_failures_are_not_swallowed()
    {
        FakeSearchService canceled = new()
        {
            SearchHandler = (_, _, _, cancellationToken) => throw new OperationCanceledException(cancellationToken)
        };
        SearchAgentTool canceledTool = CreateTool(canceled);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await FluentActions.Invoking(() => canceledTool.ExecuteAsync(Call("""{"query":"visit"}"""), new("run-1"), cancellation.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        FakeSearchService failed = new()
        {
            SearchHandler = (_, _, _, _) => throw new InvalidOperationException("search dependency failed")
        };
        SearchAgentTool failedTool = CreateTool(failed);

        await FluentActions.Invoking(() => failedTool.ExecuteAsync(Call("""{"query":"visit"}"""), new("run-1")))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("search dependency failed");
    }

    [Fact]
    public async Task Invalid_search_response_is_rejected()
    {
        FakeSearchService service = new()
        {
            SearchHandler = (_, _, _, _) => Task.FromResult<SearchResult>(null!)
        };
        SearchAgentTool tool = CreateTool(service);

        await FluentActions.Invoking(() => tool.ExecuteAsync(Call("""{"query":"visit"}"""), new("run-1")))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid result envelope*");
    }

    private static SearchAgentTool CreateTool(FakeSearchService service, SearchAgentOptions? options = null) =>
        new(service, options ?? new());

    private static AgentToolCall Call(string arguments) => new("call-1", "search", arguments);

    private sealed class FakeSearchService : ISearchService
    {
        public IReadOnlyList<SearchIndexPolicyReport> Reports { get; init; } =
        [
            new() { IndexName = "events", ExactMatchPolicy = SearchExactMatchPolicy.Disabled },
            new() { IndexName = "notes", ExactMatchPolicy = SearchExactMatchPolicy.Disabled }
        ];

        public List<SearchCall> Calls { get; } = [];
        public SearchOptions? LastOptions { get; private set; }
        public string? LastIndex { get; private set; }
        public Func<string?, string, SearchOptions, CancellationToken, Task<SearchResult>> SearchHandler { get; init; } =
            (indexName, query, _, _) => Task.FromResult(new SearchResult
            {
                Items =
                [
                    new()
                    {
                        Id = "doc-1", Title = "Appointment", Body = "private body", SemanticText = "private semantic text",
                        TenantId = "tenant-secret", Type = "event", IndexName = indexName, Metadata = new Dictionary<string, object?> { ["secret"] = "value" }
                    }
                ],
                TotalCount = 1,
                Query = query
            });

        public Task<SearchResult> SearchAsync(string? indexName, string query, SearchOptions options, CancellationToken cancellationToken = default)
        {
            LastIndex = indexName;
            LastOptions = options;
            Calls.Add(new(indexName, query));
            return SearchHandler(indexName, query, options, cancellationToken);
        }

        public IReadOnlyList<SearchIndexPolicyReport> GetIndexPolicyReports() => Reports;
        public Task IndexAsync(string indexName, IEnumerable<SearchDocument> documents, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string indexName, SearchDeleteOptions options, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RebuildIndexAsync(string indexName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<IndexStatusReport>> GetIndexStatusAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed record SearchCall(string? IndexName, string Query);
}
