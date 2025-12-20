using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Hosting.Middleware;

namespace Vitarish.Main.Services
{
    internal static class RequestsContextExtensions
    {

        public static async Task PopulateContext(HttpContext http, IRequestContextWriter writer)
        {
            ClaimsPrincipal user = http.User;

            // Generate or retrieve request ID
            string? existingReqId = http.Request.Headers[RequestIdentityConstants.RequestIdHeader].FirstOrDefault();
            string requestId = existingReqId ?? Guid.NewGuid().ToString();

            // Set the request ID header on the response
            if (!http.Response.HasStarted)
            {
                http.Response.Headers[RequestIdentityConstants.RequestIdHeader] = requestId;
            }

            // Get authority from options if available, otherwise use issuer claim
            IOptions<RequestsOptions>? options = http.RequestServices.GetService<IOptions<RequestsOptions>>();
            string? authority = options?.Value?.Authority ?? user.FindFirstValue("iss") ?? string.Empty;

            // Get client_id from claims (client_id claim is standard in OAuth2/OIDC access tokens)
            RouteValueDictionary routeValues = http.GetRouteData()?.Values ?? [];
            string? routeClientId = routeValues["clientId"]?.ToString();
            string? claimClientId = user.FindFirstValue("client_id");
            string clientId = claimClientId ?? routeClientId ?? string.Empty;

            // Get owner_id and tenant_id from claims (trusted source)
            string? claimOwnerId = user.FindFirstValue(RequestIdentityConstants.OwnerIdClaim);
            string? claimTenantId = user.FindFirstValue(RequestIdentityConstants.TenantIdClaim);

            // If claims are missing, attempt to resolve tenant and owner from the request host
            string ownerId = claimOwnerId ?? string.Empty;
            string? tenantId = claimTenantId;

            if (string.IsNullOrEmpty(ownerId) || string.IsNullOrEmpty(tenantId))
            {
                ITenantsService? tenantsService = http.RequestServices.GetService<ITenantsService>();
                if (tenantsService is not null)
                {
                    Tenant? tenant = await tenantsService.ResolveFromHostAsync(http, http.RequestAborted);
                    if (tenant is not null)
                    {
                        ownerId = ownerId.Length == 0 ? tenant.OwnerId.ToString() : ownerId;
                        tenantId ??= tenant.Id.ToString();

                        if (string.IsNullOrEmpty(clientId))
                        {
                            clientId = tenant.ClientId;
                        }
                    }
                }
            }

            // Fallback for owner_id: route value, then clientId
            string? routeOwnerId = routeValues["ownerId"]?.ToString();
            ownerId = ownerId.Length > 0 ? ownerId : routeOwnerId ?? clientId;

            // Fallback for tenant_id: synthesize from clientId and ownerId, or null if both are empty.
            // When synthesizing, if clientId is empty but ownerId is present, tenantId = ownerId.
            // If both clientId and ownerId are present, tenantId = "{clientId}.{ownerId}".
            if (string.IsNullOrEmpty(tenantId) && (!string.IsNullOrEmpty(clientId) || !string.IsNullOrEmpty(ownerId)))
            {
                tenantId = string.IsNullOrEmpty(clientId) ? ownerId : $"{clientId}.{ownerId}";
            }

            // Get resource identifier from the route pattern
            string? resourceId = (http.GetEndpoint() as RouteEndpoint)?.RoutePattern?.RawText
                                 ?? http.Request.Path.ToString();

            // Get trace ID from activity
            string? traceId = Activity.Current?.TraceId.ToHexString();

            // Get user ID from sub claim
            string? userId = user.FindFirstValue("sub");

            // Populate the context via the writer interface
            writer.SetContext(
                authority: authority,
                clientId: clientId,
                ownerId: ownerId,
                tenantId: tenantId,
                userId: userId,
                resourceId: resourceId,
                traceId: traceId,
                requestId: requestId);
        }
    }
}