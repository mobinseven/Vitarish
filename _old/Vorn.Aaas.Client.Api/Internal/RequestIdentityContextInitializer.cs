using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Contracts;
using Vorn.Aaas.Client.Api.Options;

namespace Vorn.Aaas.Client.Api.Internal;

public static class RequestIdentityContextInitializer
{
    public static async Task<IDisposable?> Initialize<TLogger>(
        HttpContext http,
        ClaimsPrincipal? user = null)
    {
        user ??= http.User;
        IServiceProvider services = http.RequestServices;
        IRequestIdentityContextWriter contextWriter = services.GetRequiredService<IRequestIdentityContextWriter>();
        //IRequestIdentityContext identityContext = services.GetRequiredService<IRequestIdentityContext>();
        IOptions<VornOptions> options = services.GetRequiredService<IOptions<VornOptions>>();
        ILogger<TLogger> logger = services.GetRequiredService<ILogger<TLogger>>();

        VornOptions vorn = options.Value;
        string? existingReqId = RequestIdentityContextHeaders.GetRequestId(http.Request.Headers);
        string requestId = existingReqId ?? Guid.NewGuid().ToString();

        if (!http.Response.HasStarted)
        {
            http.Response.Headers[RequestIdentityContextHeaders.RequestId] = requestId;
        }

        IDictionary<string, object> scopeState = new Dictionary<string, object>
        {
            ["request_id"] = requestId
        };

        IDisposable? scope = logger.BeginScope(scopeState);

        string authority = vorn.Aaas.Authority ?? throw new InvalidOperationException("AaaS Authority missing.");

        RouteValueDictionary values = http.GetRouteData()?.Values ?? [];

        string? routeClientId = values["clientId"]?.ToString();
        string? clientId = routeClientId ?? vorn.Aaas.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("AAAS client id missing.");
        }
        string? routeOwnerId = values["ownerId"]?.ToString();
        string? headerOwnerId = RequestIdentityContextHeaders.GetOwnerId(http.Request.Headers);
        string ownerId = routeOwnerId ?? headerOwnerId ?? clientId;

        string? headerTenantId = RequestIdentityContextHeaders.GetTenantId(http.Request.Headers);
        string? tenantId = headerTenantId ?? $"{clientId}.{ownerId}";

        string? resourceId = (http.GetEndpoint() as RouteEndpoint)?.RoutePattern?.RawText
                             ?? http.Request.Path.ToString();

        string? traceId = Activity.Current?.TraceId.ToHexString();

        RequestIdentityContextCommand command = new(
            authority,
            clientId,
            ownerId,
            tenantId,
            user?.FindFirstValue("sub"),
            resourceId,
            traceId,
            requestId,
            null);

        await contextWriter.Write(command);

        return scope;
    }
}
