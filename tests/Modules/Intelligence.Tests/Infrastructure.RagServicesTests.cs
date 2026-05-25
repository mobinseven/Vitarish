using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Infrastructure.Services.Rag;
using Vitarish.Search.Domain.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Intelligence.Tests;

public sealed class RagServicesTests
{
    [Fact]
    public async Task RetrieveOnlyRagService_UsesSearchBridgeResults_WhenBridgeReturnsDocuments()
    {
        // Arrange
        var bridge = new TestRagSearchBridge(
        [
            new RagRetrievedDocument
            {
                Id = "search-1",
                Content = "from-search",
                Source = "search/index",
                Score = 0.99
            }
        ]);
        var service = new RetrieveOnlyRagService(bridge);

        // Act
        RagRetrieveResult result = await service.RetrieveAsync(new RagRetrieveRequest { Query = "search", MaxResults = 5 });

        // Assert
        result.Documents.Should().ContainSingle();
        result.Documents[0].Id.Should().Be("search-1");
    }

    [Fact]
    public async Task RetrieveOnlyRagService_FallsBackToEmbeddedCorpus_WhenBridgeReturnsEmpty()
    {
        // Arrange
        var service = new RetrieveOnlyRagService(new TestRagSearchBridge([]));

        // Act
        RagRetrieveResult result = await service.RetrieveAsync(new RagRetrieveRequest { Query = "HealthCheck", MaxResults = 5 });

        // Assert
        result.Documents.Should().NotBeEmpty();
        result.Documents.Should().Contain(x => x.Content.Contains("HealthCheck", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RagHealthCheckService_ReturnsHealthy_WhenRetrievalDoesNotThrow()
    {
        // Arrange
        var retrievalService = new RetrieveOnlyRagService(new TestRagSearchBridge([]));
        var healthCheck = new RagHealthCheckService(retrievalService);

        // Act
        RagHealthReport report = await healthCheck.CheckAsync();

        // Assert
        report.IsHealthy.Should().BeTrue();
        report.Details.Should().ContainEquivalentOf("available");
    }

    [Fact]
    public async Task PlaceholderServices_ReturnPlannedStatus()
    {
        // Arrange
        var reranking = new PlaceholderRagRerankingService();
        var platformAdmin = new PlaceholderRagPlatformAdminService();

        // Act
        string rerankingStatus = await reranking.GetStatusAsync();
        string platformStatus = await platformAdmin.GetStatusAsync();

        // Assert
        rerankingStatus.Should().ContainEquivalentOf("future phase");
        platformStatus.Should().ContainEquivalentOf("deferred");
    }

    [Fact]
    public async Task GroundedGenerationRagService_ReturnsGroundedAnswerAndCitations()
    {
        // Arrange
        var retrieval = new TestRagRetrievalService(new RagRetrieveResult
        {
            Documents =
            [
                new RagRetrievedDocument
                {
                    Id = "doc-1",
                    Content = "RAG uses retrieved context.",
                    Source = "knowledge/base",
                    Score = 0.9
                }
            ]
        });
        var responder = MockResponder.WithTextResponse("Grounded answer.");
        var service = new GroundedGenerationRagService(retrieval, responder);

        // Act
        RagGroundedGenerationResult result = await service.GenerateAsync(new RagGroundedGenerationRequest
        {
            Query = "How does RAG work?",
            MaxDocuments = 3
        });

        // Assert
        result.IsGrounded.Should().BeTrue();
        result.Answer.Should().Contain("Grounded answer");
        result.Citations.Should().ContainSingle(x => x.DocumentId == "doc-1");
        responder.GenerateCalls.Should().ContainSingle();
    }

    [Fact]
    public async Task GroundedGenerationRagService_ReturnsNotGrounded_WhenNoDocumentsFound()
    {
        // Arrange
        var retrieval = new TestRagRetrievalService(new RagRetrieveResult { Documents = [] });
        var responder = MockResponder.WithTextResponse("Should not be used");
        var service = new GroundedGenerationRagService(retrieval, responder);

        // Act
        RagGroundedGenerationResult result = await service.GenerateAsync(new RagGroundedGenerationRequest
        {
            Query = "unknown",
            MaxDocuments = 2
        });

        // Assert
        result.IsGrounded.Should().BeFalse();
        result.Citations.Should().BeEmpty();
        responder.GenerateCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchRagSearchBridge_MapsSearchDocuments_WhenSearchServiceAvailable()
    {
        // Arrange
        var searchService = new TestSearchService(new SearchResult
        {
            Items =
            [
                new SearchDocument
                {
                    Id = "s1",
                    Title = "Doc title",
                    Body = "Doc body",
                    TenantId = "t1",
                    Type = "note",
                    IndexName = "index-a"
                }
            ],
            TotalCount = 1,
            Query = "doc"
        });

        var bridge = new SearchRagSearchBridge(new StaticServiceProvider(searchService));

        // Act
        IReadOnlyList<RagRetrievedDocument> result = await bridge.RetrieveAsync(new RagRetrieveRequest
        {
            Query = "doc",
            MaxResults = 5
        });

        // Assert
        result.Should().ContainSingle();
        result[0].Id.Should().Be("s1");
        result[0].Source.Should().Be("index-a:note");
    }

    [Fact]
    public async Task SearchRagSearchBridge_ReturnsEmpty_WhenSearchServiceMissing()
    {
        // Arrange
        var bridge = new SearchRagSearchBridge(new StaticServiceProvider(service: null));

        // Act
        IReadOnlyList<RagRetrievedDocument> result = await bridge.RetrieveAsync(new RagRetrieveRequest
        {
            Query = "doc",
            MaxResults = 2
        });

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void RagCapabilityCatalog_ReportsV1AndPlannedCapabilities()
    {
        // Arrange
        var catalog = new RagCapabilityCatalog();

        // Act
        IReadOnlyList<RagCapabilityDescriptor> capabilities = catalog.GetCapabilities();

        // Assert
        capabilities.Should().Contain(x => x.Capability == RagCapability.RetrieveOnly && x.State == RagCapabilityState.Available);
        capabilities.Should().Contain(x => x.Capability == RagCapability.HealthCheck && x.State == RagCapabilityState.Available);
        capabilities.Should().Contain(x => x.Capability == RagCapability.GroundedGeneration && x.State == RagCapabilityState.Available);
        capabilities.Should().Contain(x => x.Capability == RagCapability.PlatformAdmin && x.State == RagCapabilityState.Planned);
    }

    private sealed class TestRagSearchBridge(IReadOnlyList<RagRetrievedDocument> documents) : IRagSearchBridge
    {
        public Task<IReadOnlyList<RagRetrievedDocument>> RetrieveAsync(RagRetrieveRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(documents);
    }

    private sealed class TestRagRetrievalService(RagRetrieveResult result) : IRagRetrievalService
    {
        public Task<RagRetrieveResult> RetrieveAsync(RagRetrieveRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }

    private sealed class TestSearchService(SearchResult searchResult) : ISearchService
    {
        public Task IndexAsync(string indexName, IEnumerable<SearchDocument> documents, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<SearchResult> SearchAsync(string? indexName, string query, SearchOptions options, CancellationToken cancellationToken = default)
            => Task.FromResult(searchResult);

        public Task RebuildIndexAsync(string indexName, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public IReadOnlyList<SearchIndexPolicyReport> GetIndexPolicyReports() => [];

        public Task<IReadOnlyList<IndexStatusReport>> GetIndexStatusAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<IndexStatusReport>>([]);
    }

    private sealed class StaticServiceProvider(ISearchService? service) : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => serviceType == typeof(ISearchService) ? service : null;
    }
}
