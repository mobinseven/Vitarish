using Vitarish.Intelligence.Application.Abstractions;

namespace Vitarish.Intelligence.Infrastructure.Services.Rag;

public sealed class PlaceholderRagRerankingService : IRagRerankingService
{
    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult("Planned for future phase. Not part of RAG v1.");
}
