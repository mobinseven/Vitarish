using Finbuckle.MultiTenant;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Appointments.Infrastructure.Data;
using Vitarish.Apps.Aggregator;
using Vitarish.Audit.Server;
using Vitarish.Commerce.Infrastructure.Data;
using Vitarish.Communications.Infrastructure.Data;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Feed.Infrastructure;
using Vitarish.Files.Infrastructure.Data;
using Vitarish.Main.Authorization;
using Vitarish.Main.Components;
using Vitarish.Main.DependencyInjection;
using Vitarish.Main.Infrastructure.BackgroundJobs;
using Vitarish.Main.Infrastructure.Handlers;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Main.Infrastructure.Services;
using Vitarish.Modules.Base;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Hosting.DependencyInjection;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Requests.Hosting.DependencyInjection;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Users.Hosting.DependencyInjection;
using Vitarish.Users.Infrastructure.Data;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

void configureSqlServer(DbContextOptionsBuilder options)
{
    _ = options.UseSqlServer(
    builder.Configuration.GetConnectionString("Db"),
    b =>
    {
        _ = b.MigrationsAssembly("Vitarish.Main");
        _ = b.EnableRetryOnFailure();
    });
}

_ = builder.Services.AddScoped<ITenantService, TenantService>();
_ = builder.Services.AddAuditInfrastructure(configureSqlServer);

await builder.RegisterApps(configureSqlServer);
await builder.RegisterModules(configureSqlServer);

// Register Feed infrastructure
_ = builder.Services.AddFeedInfrastructure();

builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddHubOptions(options =>
                {
                    options.EnableDetailedErrors = true;
                    options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
                    options.MaximumParallelInvocationsPerClient = 1;
                    options.MaximumReceiveMessageSize = 128 * 1024;
                });

_ = builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/Pages";
});

builder.AddAccountsInterface();
builder.AddApplicationOptions();

builder.AddUsers(configureSqlServer);
builder.AddRequests(configureSqlServer);
builder.AddParties(configureSqlServer);

await builder.AddHostIdentityServer(configureSqlServer);

builder.AddHostAuthentication();
builder.AddHostAuthorization();
builder.AddHostHangfire(builder.Configuration.GetConnectionString("Db")!);
builder.AddHostPipeline();

await builder.AddHostDataProtection();

_ = builder.Services.AddMemoryCache();
_ = builder.Services.AddHttpContextAccessor();
_ = builder.Services.AddDistributedMemoryCache();
_ = builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(2);  // Align with WebAuthn timeout (60s) + buffer
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
_ = builder.Services.AddControllers();
_ = builder.Services.AddEndpointsApiExplorer();
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
builder.Services.AddVitarishInterfacesBase();

builder.Services.AddSingleton<IAppsProvider, AppsProvider>();

builder.Services.AddScoped<EntityDataProvider>();
builder.Services.AddScoped<IEntityUserService>(sp => sp.GetRequiredService<EntityDataProvider>());
builder.Services.AddScoped<IEntityTimeService>(sp => sp.GetRequiredService<EntityDataProvider>());

builder.Services.AddSingleton<INotificationHandler<EntityNotification>, EntityNotificationHandler>();

WebApplication app = builder.Build();

_ = await app.MigrateDatabaseAsync<UsersDbContext>();
_ = await app.MigrateDatabaseAsync<PartiesDbContext>();
_ = await app.MigrateDatabaseAsync<FilesDbContext>();
_ = await app.MigrateDatabaseAsync<RemindersDbContext>();
_ = await app.MigrateDatabaseAsync<SeaDbContext>();
_ = await app.MigrateDatabaseAsync<AuditDbContext>();
_ = await app.MigrateDatabaseAsync<AppointmentsDbContext>();
//_ = await app.MigrateDatabaseAsync<CalendarDbContext>();
_ = await app.MigrateDatabaseAsync<CommerceDbContext>();

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
app.UseStatusCodePagesWithReExecute("/Error");
_ = app.UseHsts();
_ = app.UseHttpsRedirection();
_ = app.MapStaticAssets();

_ = app.UseRouting();

_ = app.UseRateLimiter();

_ = app.UseSession();

_ = app.UseIdentityServer();

_ = app.UseAuthentication();
_ = app.UseAuthorization();

_ = app.UseAntiforgery();

_ = app.UseMultiTenant();

await app.InitializeModules();
await app.InitializeApps();

_ = app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
_ = app.MapRazorPages();

app.MapAccountsInterface();
await app.InitializeUsers();

app.MapHangfireDashboard();

app.ScheduleSelfTriggeringJobs();


app.Run();
