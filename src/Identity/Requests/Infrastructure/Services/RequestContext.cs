using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Vitarish.Requests.Application;

namespace Vitarish.Requests.Infrastructure.Services;

/// <summary>
/// A scoped implementation of <see cref="IRequestContext"/> that is populated
/// by middleware from <see cref="Microsoft.AspNetCore.Http.HttpContext.User"/> claims,
/// or manually via <see cref="IRequestIdentityContextWriter"/> for non-HTTP scenarios.
/// </summary>
public sealed class RequestContext : IRequestContext, IRequestContextWriter
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private bool initializedFromHttpContext;
    private bool overridden;
    private string authority = string.Empty;
    private string clientId = string.Empty;
    private string ownerId = string.Empty;
    private string? userId;
    private string? tenantId;
    private string? resourceId;
    private string? traceId;
    private string? requestId;
    private string? additionalInfo;

    public RequestContext(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public string Authority
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return authority;
        }
        internal set => authority = value;
    }

    /// <inheritdoc />
    public string ClientId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return clientId;
        }
        internal set => clientId = value;
    }

    /// <inheritdoc />
    public string OwnerId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return ownerId;
        }
        internal set => ownerId = value;
    }

    /// <inheritdoc />
    public string? UserId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return userId;
        }
        internal set => userId = value;
    }

    /// <inheritdoc />
    public string? TenantId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return tenantId;
        }
        internal set => tenantId = value;
    }

    /// <inheritdoc />
    public string? ResourceId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return resourceId;
        }
        internal set => resourceId = value;
    }

    /// <inheritdoc />
    public string? TraceId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return traceId;
        }
        internal set => traceId = value;
    }

    /// <inheritdoc />
    public string? RequestId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return requestId;
        }
        internal set => requestId = value;
    }

    /// <inheritdoc />
    public string? AdditionalInfo
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return additionalInfo;
        }
        internal set => additionalInfo = value;
    }

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
        overridden = true;
        initializedFromHttpContext = true;
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

    public Request GetRequest() => new()
    {
        Authority = Authority,
        ClientId = ClientId,
        OwnerId = OwnerId,
        TenantId = TenantId,
        UserId = UserId,
        ResourceId = ResourceId,
        TraceId = TraceId,
    };

    private void EnsureInitializedFromHttpContext()
    {
        if (overridden || initializedFromHttpContext)
        {
            return;
        }

        HttpContext? http = httpContextAccessor.HttpContext;
        if (http is null)
        {
            return;
        }

        ClaimsPrincipal user = http.User;

        string? existingRequestId = http.Request.Headers[RequestIdentityConstants.RequestIdHeader].FirstOrDefault();
        string resolvedRequestId = existingRequestId ?? Guid.NewGuid().ToString();

        if (!http.Response.HasStarted)
        {
            http.Response.Headers[RequestIdentityConstants.RequestIdHeader] = resolvedRequestId;
        }

        IOptions<RequestsOptions>? options = http.RequestServices.GetService<IOptions<RequestsOptions>>();
        string? resolvedAuthority = options?.Value?.Authority ?? user.FindFirstValue("iss") ?? string.Empty;

        string? claimClientId = user.FindFirstValue("client_id");
        string resolvedClientId = claimClientId ?? string.Empty;

        string? claimOwnerId = user.FindFirstValue(RequestIdentityConstants.OwnerIdClaim);
        string? claimTenantId = user.FindFirstValue(RequestIdentityConstants.TenantIdClaim);

        string resolvedOwnerId = claimOwnerId ?? string.Empty;
        string? resolvedTenantId = claimTenantId;

        if (string.IsNullOrEmpty(resolvedOwnerId) && !string.IsNullOrEmpty(resolvedClientId))
        {
            resolvedOwnerId = resolvedClientId;
        }

        if (string.IsNullOrEmpty(resolvedTenantId) && !string.IsNullOrEmpty(resolvedOwnerId))
        {
            resolvedTenantId = resolvedOwnerId;
        }

        string? resolvedUserId = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        string? resolvedResourceId = (http.GetEndpoint() as RouteEndpoint)?.RoutePattern?.RawText
                                     ?? http.Request.Path.ToString();
        string? resolvedTraceId = Activity.Current?.TraceId.ToHexString();

        Authority = resolvedAuthority;
        ClientId = resolvedClientId;
        OwnerId = resolvedOwnerId;
        TenantId = resolvedTenantId;
        UserId = resolvedUserId;
        ResourceId = resolvedResourceId;
        TraceId = resolvedTraceId;
        RequestId = resolvedRequestId;

        initializedFromHttpContext = true;
    }
}
