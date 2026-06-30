using Vitarish.Journey.Application.Models;

namespace Vitarish.Journey.Application.Abstractions;

public interface IJourneyDestinationResolver
{
    Task<string> ResolveFallbackAsync(JourneyRequest request, JourneyWorkspaceSnapshot workspace, CancellationToken cancellationToken);
}
