using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Requests.Application.Abstractions;

/// <summary>
/// Provides access to the current request's identity context, populated from HttpContext.User claims.
/// </summary>
public interface IRequestContext
{
    /// <summary>
    /// The authority (issuer) of the identity token.
    /// </summary>
    string Authority { get; }

    /// <summary>
    /// The client identifier associated with the request.
    /// </summary>
    string ClientId { get; }

    /// <summary>
    /// The owner identifier from the authenticated claims.
    /// </summary>
    string OwnerId { get; }

    /// <summary>
    /// The user identifier (subject claim) from the authenticated user.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// The tenant identifier from the authenticated claims.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// The resource identifier for the current request (typically the route pattern).
    /// </summary>
    string? ResourceId { get; }

    /// <summary>
    /// The trace identifier for distributed tracing.
    /// </summary>
    string? TraceId { get; }

    /// <summary>
    /// A unique identifier for the current request.
    /// </summary>
    string? RequestId { get; }

    /// <summary>
    /// Additional context information.
    /// </summary>
    string? AdditionalInfo { get; }

    Request GetRequest();
}
