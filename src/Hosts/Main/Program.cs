using Duende.IdentityServer.EntityFramework.DbContexts;
using Finbuckle.MultiTenant;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Vitarish.ApiClients.Hosting.Api;
using Vitarish.ApiClients.Hosting.DependencyInjection;
using Vitarish.ApiClients.Infrastructure.Data;
using Vitarish.AuthenticationClients.Hosting.Api;
using Vitarish.AuthenticationClients.Infrastructure.Data;
using Vitarish.Communications.Hosting;
using Vitarish.Entities.Server.Services;
using Vitarish.Files.Hosting;
using Vitarish.Files.Infrastructure.Data;
using Vitarish.Instagram.Hosting;
using Vitarish.Intelligence.Hosting;
using Vitarish.Main.Authorization;
using Vitarish.Main.Components;
using Vitarish.Main.DependencyInjection;
using Vitarish.Main.Infrastructure.BackgroundJobs;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Main.Infrastructure.Services;
using Vitarish.Modules.Base;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Hosting.Api;
using Vitarish.Parties.Hosting.DependencyInjection;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Requests.Hosting.Api;
using Vitarish.Requests.Hosting.DependencyInjection;
using Vitarish.Requests.Infrastructure.Data;
using Vitarish.Sea.Hosting;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Users.Hosting.Api;
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

IEnumerable<Assembly> requestedModuleAssemblies = [
            typeof(FilesModule).Assembly,
            typeof(CommunicationModule).Assembly,
            typeof(IntelligenceModule).Assembly,
            typeof(InstagramModule).Assembly,
            typeof(SeaModule).Assembly];

_ = await builder.AddModules(requestedModuleAssemblies, configureSqlServer);

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
builder.AddAaasApiClients(configureSqlServer);
builder.AddAaasAuthenticationClients(configureSqlServer);
builder.AddAaasParties(configureSqlServer);

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

        ITenantsService tenantsService = http.RequestServices.GetRequiredService<ITenantsService>();
        {
            Tenant? tenant = await tenantsService.ResolveFromHostAsync(http, http.RequestAborted);
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

_ = builder.Services.AddSignalR(o =>
{
    o.EnableDetailedErrors = true;
}).AddMessagePackProtocol();
builder.Services.AddTransient<IClaimsTransformation, ClaimsTransformation>();
builder.Services.AddVitarishInterfacesBase();
builder.Services.AddSingleton<IPackageProvider, PackageProvider>();

builder.Services.AddScoped<EntityDataProvider>();
builder.Services.AddScoped<IEntityUserService>(sp => sp.GetRequiredService<EntityDataProvider>());
builder.Services.AddScoped<IEntityTimeService>(sp => sp.GetRequiredService<EntityDataProvider>());

WebApplication app = builder.Build();

_ = await app.MigrateDatabaseAsync<UsersDbContext>();
_ = await app.MigrateDatabaseAsync<RequestsDbContext>();
_ = await app.MigrateDatabaseAsync<AuthenticationClientDbContext>();
_ = await app.MigrateDatabaseAsync<ApiClientDbContext>();
_ = await app.MigrateDatabaseAsync<PartiesDbContext>();
_ = await app.MigrateDatabaseAsync<PersistedGrantDbContext>();
_ = await app.MigrateDatabaseAsync<FilesDbContext>();
_ = await app.MigrateDatabaseAsync<SeaDbContext>();

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
_ = app.MapStaticAssets();

_ = app.UseRouting();

_ = app.UseRateLimiter();

_ = app.UseSession();

_ = app.UseIdentityServer();

_ = app.UseAuthentication();
_ = app.UseAuthorization();

_ = app.UseAntiforgery();

_ = app.MapGet("/request", (HttpContext context, IRequestContext requestContext) => requestContext.GetRequest());

_ = app.UseMultiTenant();

// Map endpoints from discovered modules
_ = await app.MapModuleEndpoints();

app.MapApiClientsEndpoints().AddEndpointAuthorization();
app.MapAuthenticationClientsEndpoints().AddEndpointAuthorization();
app.MapUserAccessEndpoints().AddEndpointAuthorization();
app.MapPersonalAccessTokenEndpoints().AddEndpointAuthorization();
app.MapUserProfileEndpoints().AddEndpointAuthorization();
app.MapWebAuthnEndpoints();

app.MapOwnerEndpoints().AddEndpointAuthorization();
app.MapTenantEndpoints().AddEndpointAuthorization();
app.MapTenantIndicatorEndpoints().AddEndpointAuthorization();
app.MapOnDemandTlsEndpoint().AddEndpointAuthorization();
app.MapAdminEndpoints().AddEndpointAuthorization();

app.MapRequestIdentityEndpoints().AddEndpointAuthorization();

_ = app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
_ = app.MapRazorPages();
app.MapAccountsInterface();

app.MapHangfireDashboard();

app.ScheduleSelfTriggeringJobs();


app.Run();
