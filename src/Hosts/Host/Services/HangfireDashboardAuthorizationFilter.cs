using Hangfire.Dashboard;

namespace Vitarish.Host.Services;

/// <summary>
/// Authorization filter for Hangfire dashboard that requires authenticated users.
/// </summary>
public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        
        // Require authenticated user
        return httpContext.User?.Identity?.IsAuthenticated ?? false;
    }
}
