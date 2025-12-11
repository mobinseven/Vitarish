namespace Vitarish.Clients.Gateway.Abstractions;

/// <summary>
/// Provides access to client identity context information sourced from options and claims.
/// This is the client-side alternative to the server-side IRequestIdentityContext.
/// </summary>
public interface IClientIdentityContext
{
    /// <summary>
    /// The authority (issuer) URL from configuration or claims.
    /// </summary>
    string Authority { get; }

    /// <summary>
    /// The client identifier from configuration.
    /// </summary>
    string ClientId { get; }

    /// <summary>
    /// The owner identifier from claims or configuration.
    /// </summary>
    string OwnerId { get; }

    /// <summary>
    /// The user identifier from claims (sub claim).
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// The tenant identifier from claims or synthesized from clientId and ownerId.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// The resource identifier for the current request.
    /// </summary>
    string? ResourceId { get; }

    /// <summary>
    /// A unique identifier for the current request.
    /// </summary>
    string? RequestId { get; }
}
