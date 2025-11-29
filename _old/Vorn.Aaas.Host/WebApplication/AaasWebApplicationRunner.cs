using System.Threading.Tasks;
using Vorn.Aaas.ApiClients.Presentation.Api;
using Vorn.Aaas.AuthenticationClients.Presentation.Api;
using Vorn.Aaas.Client.Api.EndpointFilter;
using Vorn.Aaas.RequestIdentities.Infrastructure.Data;
using Vorn.Aaas.RequestIdentities.Presentation.Api;
using Vorn.Aaas.Users.Infrastructure.Data;
using Vorn.Aaas.Users.Presentation.Api;

public class AaasWebApplicationRunner(WebApplication app)
{
    public async Task Run()
    {
        app.UseDashboardServices();

        await app.MigrateDatabaseAsync<UsersDbContext>();
        await app.MigrateDatabaseAsync<RequestIdentityDbContext>();

        var cookiePolicyOptions = new CookiePolicyOptions
        {
            Secure = CookieSecurePolicy.Always
        };
        var localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture("fa")
            .AddSupportedCultures(["en", "fa"])
            .AddSupportedUICultures(["en", "fa"]);

        app.UseRequestLocalization(localizationOptions);
        app.UseCookiePolicy(cookiePolicyOptions);

        app.UseExceptionHandler("/Error");
        app.UseHsts();
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseIdentityServer();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        app.MapApiClientsEndpoints(group => group
            .AddEndpointFilter<RequestIdentityContextEndpointFilter>());
        app.MapAuthenticationClientsEndpoints(group => group
            .AddEndpointFilter<RequestIdentityContextEndpointFilter>());
        app.MapUserAccessEndpoints(group => group
            .AddEndpointFilter<RequestIdentityContextEndpointFilter>());
        app.MapPersonalAccessTokenEndpoints(group => group
            .AddEndpointFilter<RequestIdentityContextEndpointFilter>());
        app.MapRequestIdentityEndpoints();
        app.MapAaasInterface();

        app.Run();
    }
}
