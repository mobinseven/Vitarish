using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Domain.Models;

namespace Vitarish.Intelligence.Infrastructure.Services.Rag;

public sealed class NullRagSearchBridge : IRagSearchBridge
{
    public Task<IReadOnlyList<RagRetrievedDocument>> RetrieveAsync(
        RagRetrieveRequest request,
        CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<RagRetrievedDocument>>([]);
}
