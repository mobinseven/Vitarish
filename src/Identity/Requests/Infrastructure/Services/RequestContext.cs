using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;
using Vitarish.Requests.Application;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Requests.Infrastructure.Services;

/// <summary>
/// A scoped implementation of <see cref="IRequestContext"/> that is populated
/// by middleware from <see cref="Microsoft.AspNetCore.Http.HttpContext.User"/> claims,
/// or manually via <see cref="IRequestIdentityContextWriter"/> for non-HTTP scenarios.
/// </summary>
public sealed class RequestContext(IHttpContextAccessor httpContextAccessor) : IRequestContext, IRequestContextWriter
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor;
    private bool initializedFromHttpContext;
    private bool overridden;

    /// <inheritdoc />
    public string Authority
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return field;
        }
        internal set;
    } = string.Empty;

    /// <inheritdoc />
    public string? ClientId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return field;
        }
        internal set;
    }

    /// <inheritdoc />
    public string? OwnerId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return field;
        }
        internal set;
    }

    /// <inheritdoc />
    public string? UserId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return field;
        }
        internal set;
    }

    /// <inheritdoc />
    public string? TenantId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return field;
        }
        internal set;
    }

    /// <inheritdoc />
    public string? ResourceId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return field;
        }
        internal set;
    }

    /// <inheritdoc />
    public string? TraceId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return field;
        }
        internal set;
    }

    /// <inheritdoc />
    public string? RequestId
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return field;
        }
        internal set;
    }

    /// <inheritdoc />
    public string? AdditionalInfo
    {
        get
        {
            EnsureInitializedFromHttpContext();
            return field;
        }
        internal set;
    }



    ClaimsPrincipal? IRequestContext.User => httpContextAccessor.HttpContext?.User;
    /// <inheritdoc />
    public void SetContext(
        string authority,
        string? clientId,
        string? ownerId,
        string? tenantId = null,
        string? userId = null,
        string? resourceId = null,
        string? traceId = null,
        string? requestId = null,
        string? additionalInfo = null,
       string? metadata = null)
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

        string? resolvedClientId = http.GetClientId();
        string? resolvedOwnerId = http.GetOwnerId();
        string? resolvedTenantId = http.GetTenantId();

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
