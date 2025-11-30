using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Vitarish.Identity.Core.Abstractions;
using Vitarish.Identity.Infrastructure.Context;

namespace Vitarish.Identity.Api.Extensions;

/// <summary>
/// Extension methods for <see cref="HttpContext"/> to work with identity context.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the identity context from the HTTP context.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>An <see cref="IIdentityContext"/> instance.</returns>
    public static IIdentityContext GetIdentityContext(this HttpContext httpContext)
    {
        return new ClaimsIdentityContext(httpContext.User);
    }

    /// <summary>
    /// Gets the admin context from the HTTP context.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>An <see cref="IAdminContext"/> instance.</returns>
    public static IAdminContext GetAdminContext(this HttpContext httpContext)
    {
        return new ClaimsAdminContext(httpContext.User);
    }

    /// <summary>
    /// Gets the current user's ID from the HTTP context.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>The user ID, or null if not authenticated.</returns>
    public static string? GetUserId(this HttpContext httpContext)
    {
        return httpContext.GetIdentityContext().UserId;
    }

    /// <summary>
    /// Checks if the current user is authenticated.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <returns>True if the user is authenticated.</returns>
    public static bool IsUserAuthenticated(this HttpContext httpContext)
    {
        return httpContext.User.Identity?.IsAuthenticated ?? false;
    }
}
