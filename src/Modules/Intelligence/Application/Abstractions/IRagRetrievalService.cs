using Vitarish.Intelligence.Domain.Models;

namespace Vitarish.Intelligence.Application.Abstractions;

public interface IRagRetrievalService
{
    Task<RagRetrieveResult> RetrieveAsync(RagRetrieveRequest request, CancellationToken cancellationToken = default);
}
