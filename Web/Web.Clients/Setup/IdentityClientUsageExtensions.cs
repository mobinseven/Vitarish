using Microsoft.AspNetCore.Builder;
using Vitarish.Web.Clients.Extensions;

namespace Vitarish.Web.Clients.Setup;

/// <summary>
/// Extension methods for using identity client middleware.
/// </summary>
public static class IdentityClientUsageExtensions
{
    /// <summary>
    /// Adds identity client middleware to the application pipeline.
    /// </summary>
    public static WebApplication UseIdentityClient(this WebApplication app)
    {
        _ = app.UseHsts();
        _ = app.UseHttpsRedirection();
        _ = app.UseStaticFiles();
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        _ = app.UseAntiforgery();
        _ = app.MapGroup("/authentication").MapLoginAndLogout();
        return app;
    }
}
