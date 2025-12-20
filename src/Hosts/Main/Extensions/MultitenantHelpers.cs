using Microsoft.AspNetCore.Http;
using Vitarish.Requests.Application.Abstractions;

public static class MultitenantHelpers
{
    /// <summary>
    /// Resolves the tenant identifier from the request context.
    /// Uses IHttpContextAccessor to ensure we get the IRequestContext from the correct request scope,
    /// which is populated by RequestIdentityContextMiddleware.
    /// </summary>
    /// <param name="context">The HTTP context (may be from Finbuckle delegate strategy).</param>
    /// <returns>The tenant identifier, owner identifier, or client identifier.</returns>
    public static Task<string?> ResolveTenantAsync(HttpContext context)
    {
        if (context is null)
        {
            return Task.FromResult<string?>(null);
        }

        // Get IHttpContextAccessor to ensure we use the correct request-scoped HttpContext.
        // The context parameter from Finbuckle's delegate may not have the same service scope
        // as the one populated by RequestIdentityContextMiddleware.
        IHttpContextAccessor? httpContextAccessor = context.RequestServices.GetService<IHttpContextAccessor>();
        HttpContext? currentContext = httpContextAccessor?.HttpContext ?? context;

        IRequestContext ric = currentContext.RequestServices.GetRequiredService<IRequestContext>();

        return !string.IsNullOrEmpty(ric.TenantId)
            ? Task.FromResult<string?>(ric.TenantId)
            : !string.IsNullOrEmpty(ric.OwnerId)
                ? Task.FromResult<string?>(ric.OwnerId)
                : Task.FromResult<string?>(ric.ClientId);
    }
}
