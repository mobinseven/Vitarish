using Finbuckle.MultiTenant;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Audit.Server;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Feed.Infrastructure;
using Vitarish.Main.Authorization;
using Vitarish.Main.DependencyInjection;
using Vitarish.Main.Infrastructure.BackgroundJobs;
using Vitarish.Main.Infrastructure.Entities;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Platform.Module.DependencyInjection;
using Vitarish.Modules.Base.Core;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Apps.Aggregator.Core;
using Vitarish.Landing.Application;
using Microsoft.Extensions.Options;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

void configurePostgres(DbContextOptionsBuilder options)
{
    string? assemblyName = options.Options.ContextType.Assembly.FullName;
    if (assemblyName is null || !assemblyName.StartsWith("Vitarish", StringComparison.Ordinal))
    {
        assemblyName = "Vitarish.Main";
    }

    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Db"),
        b =>
        {
            _ = b.MigrationsAssembly(assemblyName);
            _ = b.EnableRetryOnFailure();
        });
}

_ = builder.Services.AddScoped<ITenantService, TenantService>();
_ = builder.Services.AddAuditInfrastructure(configurePostgres);

await builder.RegisterApps(configurePostgres);
await builder.RegisterModules(configurePostgres);

// Register Feed infrastructure
_ = builder.Services.AddFeedInfrastructure();

builder.AddApplicationOptions();
_ = builder.Services.AddOptions<LandingPlatformOptions>()
    .Configure<IOptions<ApplicationOptions>>((landing, platform) =>
        landing.LandingUrl = platform.Value.LandingUrl);


await builder.AddHostIdentityServer(configurePostgres);

builder.AddHostAuthentication();
builder.AddHostAuthorization();
builder.AddHostHangfire(builder.Configuration.GetConnectionString("Db")!);
builder.AddHostPipeline();
builder.AddHostCors();

await builder.AddHostDataProtection();

_ = builder.Services.AddMemoryCache();
_ = builder.Services.AddHttpContextAccessor();
builder.AddRedisCaching();
_ = builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(2);  // Align with WebAuthn timeout (60s) + buffer
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
_ = builder.Services.AddControllers();
_ = builder.Services.AddProblemDetails();
_ = builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.None;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
_ = builder.Services.AddEndpointsApiExplorer();
// OpenAPI document generation is host-owned and exposed only in Development.
_ = builder.Services.AddOpenApi();
_ = builder.Services.AddLocalization();
Func<object, Task<string?>> tenantResolver = async context =>
{

    if (context is HttpContext http)
    {

        ITenantResolver tenantsService = http.RequestServices.GetRequiredService<ITenantResolver>();
        {
            Tenant? tenant = await tenantsService.ResolveAsync(http, http.RequestAborted);
            if (tenant is not null)
            {
                return tenant.Id.ToString();
            }
        }
    }
    else
    {
        throw new InvalidOperationException("context is not HttpContext");
    }

    return null;
}
;
_ = builder.Services.AddMultiTenant<TenantInfo>()
    .WithEchoStore()
    .WithDelegateStrategy(tenantResolver);

builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformation>();

builder.Services.AddScoped<IEntityUserService, RequestEntityUserService>();
builder.Services.AddScoped<IEntityTimeService, UtcEntityTimeService>();

WebApplication app = builder.Build();

_ = await app.MigrateDatabaseAsync<AuditDbContext>();

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

_ = app.UseExceptionHandler();
_ = app.UseHsts();
_ = app.UseHttpsRedirection();

_ = app.UseRouting();

_ = app.UseCors("WebHostCors");
app.UseHostWebSockets();

_ = app.UseRateLimiter();

_ = app.UseSession();

_ = app.UseIdentityServer();

_ = app.UseAuthentication();
_ = app.UseAuthorization();

// Resolve tenant context before centralized module antiforgery so expected security-validation logs and ProblemDetails can include tenant-aware request services when needed.
_ = app.UseMultiTenant();

_ = app.UseModuleEndpointAntiforgery();

if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi("/openapi/{documentName}.json");
}

await app.InitializeModules();
await app.InitializeApps();

app.MapHangfireDashboard();

// OpenAPI extraction executes the host to discover endpoint metadata. Recurring
// job registration is an operational side effect and is not required for that
// discovery; suppress it only when the extractor explicitly opts in.
if (!string.Equals(Environment.GetEnvironmentVariable("VITARISH_OPENAPI_GENERATION"), "true", StringComparison.OrdinalIgnoreCase))
{
    app.ScheduleJobs();
}


app.Run();
