using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Infrastructure.Services.Rag;

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
        var grounded = new PlaceholderRagGroundedGenerationService();
        var reranking = new PlaceholderRagRerankingService();
        var platformAdmin = new PlaceholderRagPlatformAdminService();

        // Act
        string groundedStatus = await grounded.GetStatusAsync();
        string rerankingStatus = await reranking.GetStatusAsync();
        string platformStatus = await platformAdmin.GetStatusAsync();

        // Assert
        groundedStatus.Should().ContainEquivalentOf("future phase");
        rerankingStatus.Should().ContainEquivalentOf("future phase");
        platformStatus.Should().ContainEquivalentOf("deferred");
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
        capabilities.Should().Contain(x => x.Capability == RagCapability.PlatformAdmin && x.State == RagCapabilityState.Planned);
    }

    private sealed class TestRagSearchBridge(IReadOnlyList<RagRetrievedDocument> documents) : IRagSearchBridge
    {
        public Task<IReadOnlyList<RagRetrievedDocument>> RetrieveAsync(RagRetrieveRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(documents);
    }
}
