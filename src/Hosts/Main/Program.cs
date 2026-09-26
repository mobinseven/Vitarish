using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Vitarish.Audit.Domain;
using Vitarish.Audit.Server;
using Vitarish.Hosting;
using Vitarish.Main.DependencyInjection;
using Vitarish.Main.Infrastructure.BackgroundJobs;
using Vitarish.Main.Infrastructure.Health;
using Vitarish.Main.Infrastructure.Options;
using Vitarish.Hosting.Infrastructure.Options;
using Vitarish.Modules.Base.Core;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Apps.Aggregator.Core;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
bool openApiGeneration = DatabaseMigrationExtensions.IsOpenApiGeneration() ||
    builder.Environment.EnvironmentName.Equals("OpenApiGeneration", StringComparison.OrdinalIgnoreCase);
SecurityConfigurationOptions securityConfiguration = SecurityConfigurationValidator.Validate(builder.Configuration, builder.Environment, openApiGeneration);

void configurePostgres(DbContextOptionsBuilder options)
{
    string? assemblyName = options.Options.ContextType.Assembly.FullName;
    if (assemblyName is null || !assemblyName.StartsWith("Vitarish", StringComparison.Ordinal))
        assemblyName = "Vitarish.Main";

    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Db"),
        database =>
        {
            _ = database.MigrationsAssembly(assemblyName);
            _ = database.EnableRetryOnFailure();
        });
}

string authority = securityConfiguration.IdentityAuthority;
string pfxPassword = builder.Configuration["Vitarish:Identity:DataProtectionPassword"]
    ?? throw new InvalidOperationException("Vitarish:Identity:DataProtectionPassword not set");
System.Security.Cryptography.X509Certificates.X509Certificate2 signingCertificate;
if (openApiGeneration)
{
    signingCertificate = HostDataProtectionExtensions.CreateOpenApiCertificate(authority, pfxPassword);
}
else
{
    string redisConnection = builder.Configuration.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("Redis connection string not configured.");
    using StackExchange.Redis.ConnectionMultiplexer redis = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(redisConnection);
    signingCertificate = await HostDataProtectionExtensions.GetOrCreateFromRedisAsync(redis, authority, pfxPassword);
}

await builder.RegisterHosting(new HostingCompositionOptions(configurePostgres, signingCertificate, securityConfiguration));
builder.AddHostHangfire(builder.Configuration.GetConnectionString("Db"));
_ = builder.Services.AddAuditCapacityAlertMonitoring(builder.Environment, openApiGeneration);
_ = builder.Services.AddOpenApi();
_ = builder.Services.AddModuleOpenApiDocument(SecurityAuditApiRoutes.DocumentName, SecurityAuditApiRoutes.DocumentTag);

if (openApiGeneration)
    builder.Services.AddOpenApiDataProtection(builder.Configuration, securityConfiguration);
else
    await builder.AddHostDataProtection(securityConfiguration);

// OpenAPI discovery validates the full service graph, including services
// whose persistence is not exercised while generating the document.
builder.AddRedisCaching();
_ = builder.Services.AddHealthChecks()
    .AddCheck<MainDependencyHealthCheck>(
        MainHealthContract.ReadinessCheckName,
        tags: [MainHealthContract.ReadinessTag]);

WebApplication app = builder.Build();
if (!openApiGeneration)
{
    _ = await app.MigrateDatabaseAsync<PersistedGrantDbContext>();
    _ = await app.MigrateDatabaseAsync<AuditDbContext>();
}

_ = app.UseHostingPipeline(securityConfiguration);
_ = app.MapHealthChecks(MainHealthContract.ReadinessPath, new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains(MainHealthContract.ReadinessTag),
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    },
    ResponseWriter = static (context, _) =>
    {
        context.Response.ContentLength = 0;
        return Task.CompletedTask;
    }
}).AllowAnonymous();
if (app.Environment.IsDevelopment())
    _ = app.MapOpenApi("/openapi/{documentName}.json");

_ = app.MapModuleEndpoints();
_ = app.MapSecurityAuditApi();
_ = app.MapAppEndpoints();

if (!openApiGeneration)
{
    _ = await app.InitializeModules();
    _ = await app.InitializeApps();
    app.ScheduleJobs();
}

app.MapHangfireDashboard();
app.Run();
