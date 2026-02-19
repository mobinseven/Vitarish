using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;

namespace Vitarish.Main.Authorization;

/// <summary>
/// Authorization filter for Hangfire dashboard that requires authenticated users.
/// </summary>
public class HangfireDashboardAuthorizationFilter: IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        HttpContext httpContext = context.GetHttpContext();
        IAuthorizationService authorizationService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();
        AuthorizationResult res = authorizationService.AuthorizeAsync(httpContext.User, IdentityDefinitions.PlatformAdmin).ConfigureAwait(false).GetAwaiter().GetResult();
        return res.Succeeded;
    }
}
