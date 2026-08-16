using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Vitarish.Hosting;
using Vitarish.E2e.Infrastructure.Fixtures;
using Vitarish.E2e.Infrastructure.Data;
using Vitarish.E2e.Infrastructure;
using Vitarish.Apps.Aggregator.Core;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Hosting.Infrastructure.Options;
using Vitarish.Modules.Base.Core.Platform.Idempotency;

const string apiOriginKey = "VITARISH_E2E_API_ORIGIN";
const string webOriginKey = "VITARISH_E2E_WEB_ORIGIN";
const string passwordKey = "VITARISH_E2E_PASSWORD";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.EnvironmentName.Equals("E2E", StringComparison.Ordinal))
    throw new InvalidOperationException("The E2E host requires ASPNETCORE_ENVIRONMENT=E2E.");

Uri apiOrigin = RequireLoopbackOrigin(builder.Configuration[apiOriginKey], apiOriginKey, requireHttps: true);
Uri webOrigin = RequireLoopbackOrigin(builder.Configuration[webOriginKey], webOriginKey, requireHttps: false);
string password = builder.Configuration[passwordKey] ?? throw new InvalidOperationException("E2E password is required.");
if (password.Length < 12)
    throw new InvalidOperationException("E2E password must be at least 12 characters.");

string configuredUrls = builder.Configuration["urls"] ?? string.Empty;
if (!configuredUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Any(value => Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && uri == apiOrigin))
    throw new InvalidOperationException("The E2E server URLs must include the configured API origin.");

builder.Configuration["Vitarish:Identity:Authority"] = apiOrigin.ToString().TrimEnd('/');
builder.Configuration["Vitarish:LandingUrl"] = webOrigin.ToString().TrimEnd('/');
builder.Configuration["Cors:AllowedOrigins:0"] = webOrigin.ToString().TrimEnd('/');
builder.Configuration["Vitarish:Intelligence:DefaultResponseModel"] = "e2e-test-model";
builder.Configuration["Vitarish:Intelligence:AgentRuntime:BaseUrl"] = "http://localhost:3999";
builder.Configuration["Vitarish:Files:Endpoint"] = "localhost:3998";
builder.Configuration["Vitarish:Files:AccessKey"] = "e2e-access-key";
builder.Configuration["Vitarish:Files:SecretKey"] = "e2e-secret-key";
builder.Configuration["Vitarish:Files:BucketName"] = "e2e";
builder.Configuration["Vitarish:Search:Url"] = "http://localhost:3997";
builder.Configuration["Vitarish:Communications:Notifications:ContactEmailRecipient"] = "e2e-contact@example.test";
builder.Configuration["Vitarish:Features:SocialIntegrations"] = "true";
builder.Configuration["Vitarish:Security:MandatoryAuditEnabled"] = "true";
builder.Configuration["Vitarish:Security:Tls:RequireHttps"] = "true";
builder.Configuration["Vitarish:Security:TokenValidation:Issuer"] = apiOrigin.ToString().TrimEnd('/');
builder.Configuration["Vitarish:Security:TokenValidation:Audiences:0"] = "vitarish-api";
SecurityConfigurationOptions securityConfiguration = SecurityConfigurationValidator.Validate(builder.Configuration, builder.Environment);

Dictionary<Type, string> databaseNames = [];
void configureInMemory(DbContextOptionsBuilder options)
{
    Type contextType = options.Options.ContextType;
    if (!databaseNames.TryGetValue(contextType, out string? databaseName))
    {
        databaseName = $"vitarish-e2e-{contextType.FullName}-{Guid.NewGuid():N}";
        databaseNames[contextType] = databaseName;
    }
    options.UseInMemoryDatabase(databaseName);
}

await builder.RegisterHosting(new HostingCompositionOptions(
    configureInMemory,
    EphemeralCertificateFactory.Create(apiOrigin.Host),
    securityConfiguration));

_ = builder.Services.AddSingleton(
    typeof(IOperationReceiptStore<>),
    typeof(E2eInMemoryOperationReceiptStore<>));

_ = builder.Services.AddScoped<ClinicDbContext>(services =>
    new E2eClinicDbContext(
        services.GetRequiredService<DbContextOptions<ClinicDbContext>>(),
        services.GetRequiredService<Vitarish.Requests.Application.Abstractions.IRequestContext>(),
        services.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()));

_ = builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider().SetApplicationName(apiOrigin.Host);
_ = builder.Services.AddDistributedMemoryCache();
_ = builder.Services.AddOpenApi();
WebApplication app = builder.Build();
_ = app.UseHostingPipeline(securityConfiguration);
_ = app.MapModuleEndpoints();
_ = app.MapAppEndpoints();

await E2eFixtureSeeder.SeedAsync(app.Services, password, CancellationToken.None);
app.Run();

static Uri RequireLoopbackOrigin(string? value, string name, bool requireHttps)
{
    if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? origin) || !origin.IsLoopback || origin.AbsolutePath != "/" || origin.Query.Length != 0 || origin.Fragment.Length != 0)
        throw new InvalidOperationException($"{name} must be an absolute loopback origin with an explicit port.");
    if (requireHttps && origin.Scheme != Uri.UriSchemeHttps)
        throw new InvalidOperationException($"{name} must use HTTPS.");
    if (!requireHttps && origin.Scheme != Uri.UriSchemeHttp && origin.Scheme != Uri.UriSchemeHttps)
        throw new InvalidOperationException($"{name} must use HTTP or HTTPS.");
    return origin;
}
