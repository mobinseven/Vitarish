using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;

namespace Vitarish.Intelligence.Infrastructure.Services.Rag;

public sealed class RetrieveOnlyRagService(IRagSearchBridge searchBridge) : IRagRetrievalService
{
    private static readonly IReadOnlyList<RagRetrievedDocument> EmbeddedCorpus =
    [
        new()
        {
            Id = "rag-embedded-1",
            Content = "RAG v1 ships RetrieveOnly and HealthCheck capabilities.",
            Source = "intelligence/rag/v1",
            Score = 0.60
        },
        new()
        {
            Id = "rag-embedded-2",
            Content = "Search integration is optional for Retrieval in Intelligence module.",
            Source = "intelligence/rag/integration",
            Score = 0.50
        }
    ];

    public async Task<RagRetrieveResult> RetrieveAsync(
        RagRetrieveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new RagRetrieveResult { Documents = [] };
        }

        int maxResults = request.MaxResults > 0 ? request.MaxResults : 5;

        IReadOnlyList<RagRetrievedDocument> bridgedDocuments = await searchBridge
            .RetrieveAsync(request, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<RagRetrievedDocument> selected = bridgedDocuments.Count > 0
            ? bridgedDocuments
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .ToList()
            : EmbeddedCorpus
                .Where(x => x.Content.Contains(request.Query, StringComparison.OrdinalIgnoreCase) ||
                            x.Source.Contains(request.Query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.Score)
                .Take(maxResults)
                .ToList();

        return new RagRetrieveResult
        {
            Documents = selected
        };
    }
}
