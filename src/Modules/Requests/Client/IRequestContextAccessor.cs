using System.Collections.Generic;
using System.Security.Claims;
using Vitarish.Requests.Contracts;

namespace Vitarish.Requests.Client;

/// <summary>
/// Provides direct access to the current request context.
/// Property access resolves the current context internally and returns safe fallback values
/// when no context is available. Use <see cref="GetCurrent" /> when failure details are needed.
/// </summary>
public interface IRequestContextAccessor
{
    /// <summary>
    /// The authority (issuer) of the identity token.
    /// </summary>
    string Authority { get; }

    /// <summary>
    /// The client identifier associated with the request.
    /// </summary>
    string? ClientId { get; }

    /// <summary>
    /// The owner identifier from the authenticated claims.
    /// </summary>
    string? OwnerId { get; }

    /// <summary>
    /// The tenant identifier from the authenticated claims.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// The user identifier from the authenticated claims.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// The resource identifier for the current request.
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
    /// Additional resolved claim values that are not represented by first-class properties.
    /// </summary>
    IReadOnlyDictionary<string, string> AdditionalClaims { get; }

    /// <summary>
    /// The current claims principal when an HTTP context is available.
    /// </summary>
    ClaimsPrincipal? User { get; }

    /// <summary>
    /// Resolves the current request context and exposes failure details when resolution fails.
    /// </summary>
    RequestContextResult GetCurrent();
}
