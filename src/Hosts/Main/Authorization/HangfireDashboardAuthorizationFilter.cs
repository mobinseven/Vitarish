using Hangfire.Dashboard;

namespace Vitarish.Main.Authorization;

/// <summary>
/// Authorization filter for Hangfire dashboard that requires authenticated users.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        HttpContext httpContext = context.GetHttpContext();
        return httpContext.User?.Identity?.IsAuthenticated ?? false;
    }
}
