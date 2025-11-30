using Vorn.Aaas.ApiClients.Infrastructure.Data;
using Vorn.Aaas.ApiClients.Presentation.Api;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;
using Vorn.Aaas.AuthenticationClients.Presentation.Api;
using Vorn.Aaas.Client.Api.Filters;
using Vorn.Aaas.Host.Infrastructure.BackgroundJobs;
using Vorn.Aaas.Parties.Infrastructure.Data;
using Vorn.Aaas.Parties.Presentation.Api;
using Vorn.Aaas.RequestIdentities.Infrastructure.Data;
using Vorn.Aaas.RequestIdentities.Presentation.Api;
using Vorn.Aaas.Users.Infrastructure.Data;
using Vorn.Aaas.Users.Presentation.Api;

public class AaasWebApplicationRunner(WebApplication app)
{
    public async Task Run()
    {
        app.UseDashboardServices();

        _ = await app.MigrateDatabaseAsync<UsersDbContext>();
        _ = await app.MigrateDatabaseAsync<RequestIdentityDbContext>();
        _ = await app.MigrateDatabaseAsync<AuthenticationClientDbContext>();
        _ = await app.MigrateDatabaseAsync<ApiClientDbContext>();
        _ = await app.MigrateDatabaseAsync<PartiesDbContext>();

        CookiePolicyOptions cookiePolicyOptions = new()
        {
            Secure = CookieSecurePolicy.Always
        };
        RequestLocalizationOptions localizationOptions = new RequestLocalizationOptions()
            .SetDefaultCulture("fa")
            .AddSupportedCultures(["en", "fa"])
            .AddSupportedUICultures(["en", "fa"]);

        _ = app.UseForwardedHeaders();
        _ = app.UseRequestLocalization(localizationOptions);
        _ = app.UseCookiePolicy(cookiePolicyOptions);

        _ = app.UseExceptionHandler("/Error");
        _ = app.UseHsts();
        _ = app.UseHttpsRedirection();
        _ = app.UseStaticFiles();

        _ = app.UseRouting();

        _ = app.UseRateLimiter();

        _ = app.UseIdentityServer();

        _ = app.UseAuthentication();
        _ = app.UseAuthorization();

        _ = app.UseAntiforgery();

        app.MapApiClientsEndpoints(group => group
            .AddEndpointFilter<RequestIdentityContextEndpointFilter>());
        app.MapAuthenticationClientsEndpoints(group => group
            .AddEndpointFilter<RequestIdentityContextEndpointFilter>());
        app.MapUserAccessEndpoints(group => group
            .AddEndpointFilter<RequestIdentityContextEndpointFilter>());
        app.MapPersonalAccessTokenEndpoints(group => group
            .AddEndpointFilter<RequestIdentityContextEndpointFilter>());
        app.MapPartiesEndpoints(group => group
            .AddEndpointFilter<RequestIdentityContextEndpointFilter>());
        app.MapRequestIdentityEndpoints();
        app.MapAaasInterface();
        app.MapHangfireDashboard();

        app.Run();
    }
}
