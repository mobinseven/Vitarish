using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace Vitarish.Web.Clients.Extensions;

/// <summary>
/// Authorization middleware result handler for Blazor applications.
/// Passes through to allow Blazor to handle authorization results.
/// </summary>
/// <remarks>
/// See: https://github.com/dotnet/aspnetcore/issues/52063#issuecomment-1817420640
/// </remarks>
public class BlazorAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    /// <summary>
    /// Handles the authorization result by passing through to the next middleware.
    /// </summary>
    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        => next(context);
}
