namespace Vitarish.Web.Clients.Constants;

/// <summary>
/// Constants for authentication and authorization.
/// </summary>
public static class AuthConstants
{
    /// <summary>
    /// Admin policy name.
    /// </summary>
    public const string Admin = nameof(Admin);

    /// <summary>
    /// Access client name for client credentials flow.
    /// </summary>
    public const string AccessClient = "AccessClient";

    /// <summary>
    /// Access claim type name.
    /// </summary>
    public const string AccessClaim = "access";

    /// <summary>
    /// Access scope name.
    /// </summary>
    public const string AccessScope = "access";

    /// <summary>
    /// Hub scope name for SignalR connections.
    /// </summary>
    public const string HubScope = "hub";
}
