using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;

namespace Vitarish.Intelligence.Infrastructure.Services.Rag;

public sealed class RagHealthCheckService(IRagRetrievalService retrievalService) : IRagHealthCheckService
{
    public async Task<RagHealthReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await retrievalService
                .RetrieveAsync(new RagRetrieveRequest { Query = "health", MaxResults = 1 }, cancellationToken)
                .ConfigureAwait(false);

            return new RagHealthReport
            {
                IsHealthy = true,
                CheckedAt = DateTimeOffset.UtcNow,
                Details = "RAG retrieval pipeline is available."
            };
        }
        catch (Exception ex)
        {
            return new RagHealthReport
            {
                IsHealthy = false,
                CheckedAt = DateTimeOffset.UtcNow,
                Details = $"RAG retrieval pipeline failed: {ex.Message}"
            };
        }
    }
}
