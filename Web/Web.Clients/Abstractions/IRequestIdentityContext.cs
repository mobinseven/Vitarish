namespace Vitarish.Web.Clients.Abstractions;

/// <summary>
/// Provides contextual information about the current request's identity.
/// </summary>
public interface IRequestIdentityContext
{
    /// <summary>
    /// The OIDC authority URL.
    /// </summary>
    string Authority { get; }

    /// <summary>
    /// The client ID.
    /// </summary>
    string ClientId { get; }

    /// <summary>
    /// The owner ID (for multi-tenant scenarios).
    /// </summary>
    string OwnerId { get; }

    /// <summary>
    /// The authenticated user's ID.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// The tenant ID (for multi-tenant scenarios).
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// The resource ID being accessed.
    /// </summary>
    string? ResourceId { get; }

    /// <summary>
    /// Trace ID for distributed tracing.
    /// </summary>
    string? TraceId { get; }

    /// <summary>
    /// Request ID for request correlation.
    /// </summary>
    string? RequestId { get; }

    /// <summary>
    /// Additional context information.
    /// </summary>
    string? AdditionalInfo { get; }
}
