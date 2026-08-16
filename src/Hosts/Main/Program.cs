using Microsoft.EntityFrameworkCore;
using Vitarish.Audit.Server;
using Vitarish.Hosting;
using Vitarish.Main.DependencyInjection;
using Vitarish.Main.Infrastructure.BackgroundJobs;
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

string authority = builder.Configuration["Vitarish:Identity:Authority"]
    ?? throw new InvalidOperationException("Vitarish:Identity:Authority not set");
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

await builder.RegisterHosting(new HostingCompositionOptions(configurePostgres, signingCertificate, null, securityConfiguration));
builder.AddHostHangfire(builder.Configuration.GetConnectionString("Db"));
_ = builder.Services.AddOpenApi();

if (openApiGeneration)
    builder.Services.AddOpenApiDataProtection(builder.Configuration);
else
    await builder.AddHostDataProtection(securityConfiguration);

if (openApiGeneration)
    _ = builder.Services.AddDistributedMemoryCache();
else
    builder.AddRedisCaching();

WebApplication app = builder.Build();
if (!openApiGeneration)
    _ = await app.MigrateDatabaseAsync<AuditDbContext>();

_ = app.UseHostingPipeline();
if (app.Environment.IsDevelopment())
    _ = app.MapOpenApi("/openapi/{documentName}.json");

_ = app.MapModuleEndpoints();
_ = app.MapAppEndpoints();

if (!openApiGeneration)
{
    _ = await app.InitializeModules();
    _ = await app.InitializeApps();
    app.ScheduleJobs();
}

app.MapHangfireDashboard();
app.Run();
