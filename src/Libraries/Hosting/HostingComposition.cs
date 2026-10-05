using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Audit.Server;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Feed.Infrastructure;
using Vitarish.Main.Authorization;
using Vitarish.Main.DependencyInjection;
using Vitarish.Main.Infrastructure.Entities;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Hosting.Infrastructure.Options;
using Vitarish.Apps.Aggregator.Core;
using Vitarish.Landing.Application;
using Vitarish.Modules.Base.Core;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Hosting;

public sealed record HostingCompositionOptions(
    Action<DbContextOptionsBuilder> ConfigureDbContext,
    System.Security.Cryptography.X509Certificates.X509Certificate2 IdentityServerSigningCertificate,
    SecurityConfigurationOptions SecurityConfiguration);

public static class HostingComposition
{
    public static async Task<WebApplicationBuilder> RegisterHosting(this WebApplicationBuilder builder, HostingCompositionOptions options)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        _ = builder.Services.AddSingleton(options.SecurityConfiguration);
        _ = builder.Services.AddScoped<ITenantService, TenantService>();
        _ = builder.Services.AddAuditInfrastructure(options.ConfigureDbContext, builder.Configuration);
        await builder.RegisterApps(options.ConfigureDbContext);
        await builder.RegisterModules(options.ConfigureDbContext);
        _ = builder.Services.AddFeedInfrastructure();
        _ = builder.Services.AddSecureHttpClientDefaults();
        builder.AddApplicationOptions();
        _ = builder.Services.AddOptions<LandingPlatformOptions>().Configure<IOptions<ApplicationOptions>>((landing, platform) => landing.LandingUrl = platform.Value.LandingUrl);
        builder.AddHostIdentityServer(options.ConfigureDbContext, options.IdentityServerSigningCertificate);
        builder.AddHostAuthentication(options.SecurityConfiguration);
        builder.AddHostAuthorization();
        builder.AddHostPipeline(options.SecurityConfiguration);
        builder.AddHostCors(options.SecurityConfiguration.AllowedCorsOrigins);
        _ = builder.Services.AddMemoryCache();
        _ = builder.Services.AddHttpContextAccessor();
        _ = builder.Services.AddSession(session => { session.IdleTimeout = TimeSpan.FromMinutes(2); session.Cookie.HttpOnly = true; session.Cookie.IsEssential = true; session.Cookie.SameSite = SameSiteMode.Lax; session.Cookie.SecurePolicy = CookieSecurePolicy.Always; });
        _ = builder.Services.AddControllers();
        _ = builder.AddHostRequestExceptionHandling();
        _ = builder.Services.AddAntiforgery(antiforgery => { antiforgery.HeaderName = "X-XSRF-TOKEN"; antiforgery.Cookie.HttpOnly = true; antiforgery.Cookie.IsEssential = true; antiforgery.Cookie.SameSite = SameSiteMode.Lax; antiforgery.Cookie.SecurePolicy = CookieSecurePolicy.Always; });
        _ = builder.Services.AddEndpointsApiExplorer();
        _ = builder.Services.AddLocalization();
        Func<object, Task<string?>> tenantResolver = async context =>
        {
            if (context is not HttpContext http) throw new InvalidOperationException("context is not HttpContext");
            ITenantResolver tenants = http.RequestServices.GetRequiredService<ITenantResolver>();
            Tenant? tenant = await tenants.ResolveAsync(http, http.RequestAborted);
            return tenant?.Id.ToString();
        };
        _ = builder.Services.AddMultiTenant<TenantInfo>().WithEchoStore().WithDelegateStrategy(tenantResolver);
        builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformation>();
        builder.Services.AddScoped<IEntityUserService, RequestEntityUserService>();
        builder.Services.AddScoped<IEntityTimeService, UtcEntityTimeService>();
        return builder;
    }

    public static WebApplication UseHostingPipeline(this WebApplication app, SecurityConfigurationOptions security)
    {
        _ = app.UseForwardedHeaders();
        _ = app.UseRequestLocalization(new RequestLocalizationOptions().SetDefaultCulture("fa").AddSupportedCultures(["en", "fa"]).AddSupportedUICultures(["en", "fa"]));
        _ = app.UseCookiePolicy(new CookiePolicyOptions { Secure = CookieSecurePolicy.Always });
        _ = app.UseExceptionHandler();
        if (security.Tls.RequireHttps)
        {
            _ = app.UseHsts();
            _ = app.UseHttpsRedirection();
        }
        _ = app.UseRouting();
        _ = app.UseMultiTenant();
        _ = app.UseInternalServiceEndpointBoundary();
        _ = app.UseCors("WebHostCors");
        app.UseHostWebSockets(security);
        _ = app.UseRateLimiter();
        _ = app.UseSession();
        _ = app.UseIdentityServer();
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        _ = app.UseModuleEndpointAntiforgery();
        return app;
    }
}
