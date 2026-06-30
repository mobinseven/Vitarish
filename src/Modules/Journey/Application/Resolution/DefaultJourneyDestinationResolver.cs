using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Application.Models;
using Vitarish.Journey.Domain;

namespace Vitarish.Journey.Application.Resolution;

public sealed class DefaultJourneyDestinationResolver : IJourneyDestinationResolver
{
    public Task<string> ResolveFallbackAsync(JourneyRequest request, JourneyWorkspaceSnapshot workspace, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(workspace);

        string destination = string.IsNullOrWhiteSpace(workspace.DashboardPath)
            ? JourneyPolicy.GlobalDashboardPath
            : workspace.DashboardPath;

        return Task.FromResult(destination);
    }
}
