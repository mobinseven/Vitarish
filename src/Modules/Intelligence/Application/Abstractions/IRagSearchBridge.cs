using Vitarish.Intelligence.Domain.Models;

namespace Vitarish.Intelligence.Application.Abstractions;

/// <summary>
/// Optional integration point for Search module-backed retrieval.
/// </summary>
public interface IRagSearchBridge
{
    Task<IReadOnlyList<RagRetrievedDocument>> RetrieveAsync(RagRetrieveRequest request, CancellationToken cancellationToken = default);
}
