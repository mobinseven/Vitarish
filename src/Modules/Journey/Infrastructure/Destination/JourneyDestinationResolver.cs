using Vitarish.Journey.Application.Abstractions;

namespace Vitarish.Journey.Infrastructure.Destination;

/// <summary>
/// Resolves fallback destinations for the journey.
/// </summary>
public sealed class JourneyDestinationResolver : IJourneyDestinationResolver
{
    public string GetUserHomeUrl(string userId) => "/home";

    public string GetAppDashboardUrl(string appKey) => $"/apps/{appKey}/dashboard";

    public string GetWorkspaceDashboardUrl(string workspaceId) => $"/workspaces/{workspaceId}/dashboard";

    public string GetGlobalDashboardUrl() => "/dashboard";
}
