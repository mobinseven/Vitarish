namespace Vitarish.Requests.Application.Abstractions;

/// <summary>
/// Provides write access to the request identity context for non-HTTP scenarios
/// such as background jobs or message handlers.
/// </summary>
public interface IRequestContextWriter
{
    /// <summary>
    /// Sets all properties of the request identity context.
    /// </summary>
    /// <param name="authority">The authority (issuer) of the identity token.</param>
    /// <param name="clientId">The client identifier.</param>
    /// <param name="ownerId">The owner identifier.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="resourceId">The resource identifier.</param>
    /// <param name="traceId">The trace identifier.</param>
    /// <param name="requestId">The request identifier.</param>
    /// <param name="additionalInfo">Additional context information.</param>
    void SetContext(
        string authority,
        string clientId,
        string ownerId,
        string? tenantId = null,
        string? userId = null,
        string? resourceId = null,
        string? traceId = null,
        string? requestId = null,
        string? additionalInfo = null);
}
