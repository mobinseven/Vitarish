using Microsoft.AspNetCore.Builder;
using Vitarish.Clients.Interface.Identity.Extensions;
public static class AaasUsageExtensions
{
    public static WebApplication UseAaasClient(this WebApplication app)
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
