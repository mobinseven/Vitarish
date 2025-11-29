using Microsoft.AspNetCore.Builder;
using Vorn.Aaas.Client.Extensions;
public static class AaasUsageExtensions
{
    public static WebApplication UseAaasClient(this WebApplication app)
    {
        app.UseHsts();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.MapGroup("/authentication").MapLoginAndLogout();
        return app;
    }
}
