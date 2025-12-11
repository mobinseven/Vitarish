using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Requests.Infrastructure.Services;

/// <summary>
/// A scoped implementation of <see cref="IRequestContext"/> that is populated
/// by middleware from <see cref="Microsoft.AspNetCore.Http.HttpContext.User"/> claims,
/// or manually via <see cref="IRequestIdentityContextWriter"/> for non-HTTP scenarios.
/// </summary>
public sealed class RequestContext : IRequestContext, IRequestContextWriter
{
    /// <inheritdoc />
    public string Authority { get; internal set; } = string.Empty;

    /// <inheritdoc />
    public string ClientId { get; internal set; } = string.Empty;

    /// <inheritdoc />
    public string OwnerId { get; internal set; } = string.Empty;

    /// <inheritdoc />
    public string? UserId { get; internal set; }

    /// <inheritdoc />
    public string? TenantId { get; internal set; }

    /// <inheritdoc />
    public string? ResourceId { get; internal set; }

    /// <inheritdoc />
    public string? TraceId { get; internal set; }

    /// <inheritdoc />
    public string? RequestId { get; internal set; }

    /// <inheritdoc />
    public string? AdditionalInfo { get; internal set; }

    /// <inheritdoc />
    public void SetContext(
        string authority,
        string clientId,
        string ownerId,
        string? tenantId = null,
        string? userId = null,
        string? resourceId = null,
        string? traceId = null,
        string? requestId = null,
        string? additionalInfo = null)
    {
        Authority = authority;
        ClientId = clientId;
        OwnerId = ownerId;
        TenantId = tenantId;
        UserId = userId;
        ResourceId = resourceId;
        TraceId = traceId;
        RequestId = requestId ?? Guid.NewGuid().ToString();
        AdditionalInfo = additionalInfo;
    }
}
