namespace Vitarish.Journey.Application.Abstractions;

/// <summary>
/// Resolves fallback destinations for the journey.
/// </summary>
public interface IJourneyDestinationResolver
{
    /// <summary>
    /// Gets the user's home page URL.
    /// </summary>
    string GetUserHomeUrl(string userId);

    /// <summary>
    /// Gets the app dashboard URL for the given app key.
    /// </summary>
    string GetAppDashboardUrl(string appKey);

    /// <summary>
    /// Gets the workspace dashboard URL for the given workspace ID.
    /// </summary>
    string GetWorkspaceDashboardUrl(string workspaceId);

    /// <summary>
    /// Gets the global dashboard fallback URL.
    /// </summary>
    string GetGlobalDashboardUrl();
}
