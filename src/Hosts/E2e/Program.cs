using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vitarish.Hosting;
using Vitarish.E2e.Infrastructure.Fixtures;
using Vitarish.E2e.Infrastructure.Data;
using Vitarish.E2e.Infrastructure;
using Vitarish.Apps.Aggregator.Core;
using Vitarish.Clinic.Infrastructure.Data;
using Vitarish.Hosting.Infrastructure.Options;
using Vitarish.Modules.Base.Core.Platform.Idempotency;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Infrastructure.Options;
using Vitarish.E2e.Infrastructure.Audit;
using Vitarish.E2e.Infrastructure.Storage;
using Vitarish.E2e.Infrastructure.Events;
using Vitarish.Clinic.Application.Commands.Patients;
using Vitarish.Clinic.Application.Events;
using Vitarish.Clinic.Application.Assistant.Actions;
using Vitarish.Clinic.Application.Assistant.Surgeries;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Audit.Application;
using Vitarish.Audit.Server;
using Vitarish.Main.Infrastructure.BackgroundJobs;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.E2e.Infrastructure.Communications;
using Vitarish.Parties.Application.Addresses;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.SignIn;
using BaseModule = Vitarish.Modules.Base.Core.Module;

const string apiOriginKey = "VITARISH_E2E_API_ORIGIN";
const string webOriginKey = "VITARISH_E2E_WEB_ORIGIN";
const string platformWebOriginKey = "VITARISH_E2E_PLATFORM_ADMIN_ORIGIN";
const string platformCorsOriginKey = "Cors:AllowedOrigins:2";
const string platformWebAuthnOriginKey = "WebAuthn:Origins:1";
const string passwordKey = "VITARISH_E2E_PASSWORD";
const string smsCapturePathKey = "VITARISH_E2E_SMS_CAPTURE_PATH";
// Clinics live at <subdomain>.localhost on the web port; people sign up and create them on start.localhost,
// and sign in to any of their clinics on auth.localhost.
const string tenantBaseDomain = "localhost";
const string onboardingSubdomain = "start";
const string signInSubdomain = "auth";
const string landingSubdomain = "landing";
const string surgeryMaterializedViewRefresherTypeName = "SurgeryMaterializedViewRefresher";

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
if (!builder.Environment.EnvironmentName.Equals("E2E", StringComparison.Ordinal))
    throw new InvalidOperationException("The E2E host requires ASPNETCORE_ENVIRONMENT=E2E.");

Uri apiOrigin = RequireLoopbackOrigin(builder.Configuration[apiOriginKey], apiOriginKey, requireHttps: true);
Uri webOrigin = RequireLoopbackOrigin(builder.Configuration[webOriginKey], webOriginKey, requireHttps: false);
Uri platformWebOrigin = RequireLoopbackOrigin(builder.Configuration[platformWebOriginKey] ?? webOrigin.ToString(), platformWebOriginKey, requireHttps: false);
string password = builder.Configuration[passwordKey] ?? throw new InvalidOperationException("E2E password is required.");
string smsCapturePath = builder.Configuration[smsCapturePathKey] ?? throw new InvalidOperationException("The E2E SMS capture path is required, so no message can reach a real SMS provider.");
if (password.Length < 12)
    throw new InvalidOperationException("E2E password must be at least 12 characters.");

string configuredUrls = builder.Configuration["urls"] ?? string.Empty;
if (!configuredUrls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Any(value => Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) && uri == apiOrigin))
    throw new InvalidOperationException("The E2E server URLs must include the configured API origin.");

builder.Configuration["Vitarish:Identity:Authority"] = apiOrigin.ToString().TrimEnd('/');
// The landing host has its own address, so its platform tenant is provisioned there rather than
// sharing localhost with the E2E clinic; landing sign-in and handoff can then be tested. Only the
// host is used (tenant binding), so the URL keeps the HTTPS scheme the landing app requires while
// the browser reaches the same host over the E2E web port.
builder.Configuration["Vitarish:LandingUrl"] =
    new UriBuilder(webOrigin) { Scheme = Uri.UriSchemeHttps, Host = $"{landingSubdomain}.{tenantBaseDomain}" }.Uri.ToString().TrimEnd('/');
builder.Configuration["Cors:AllowedOrigins:0"] = webOrigin.ToString().TrimEnd('/');
builder.Configuration["Cors:AllowedOrigins:1"] = "http://tenant-b.localhost:3005";
builder.Configuration[platformCorsOriginKey] = platformWebOrigin.ToString().TrimEnd('/');
builder.Configuration[platformWebAuthnOriginKey] = platformWebOrigin.ToString().TrimEnd('/');
builder.Configuration["Vitarish:Intelligence:DefaultResponseModel"] = "e2e-test-model";
builder.Configuration["Vitarish:Intelligence:AgentRuntime:BaseUrl"] = "http://localhost:3999";
builder.Configuration["Vitarish:Files:Endpoint"] = "localhost:3998";
builder.Configuration["Vitarish:Files:AccessKey"] = "e2e-access-key";
builder.Configuration["Vitarish:Files:SecretKey"] = "e2e-secret-key";
builder.Configuration["Vitarish:Files:BucketName"] = "e2e";
builder.Configuration["Vitarish:Search:Url"] = "http://localhost:3997";
builder.Configuration["Vitarish:Communications:Notifications:ContactEmailRecipient"] = "e2e-contact@example.test";
builder.Configuration["Vitarish:Features:SocialIntegrations"] = "true";
builder.Configuration[BaseModule.TenantOnboardingFeatureKey] = "true";
builder.Configuration[$"{BaseModule.ApplicationSection}:{nameof(TenantAddressOptions.TenantBaseDomain)}"] = tenantBaseDomain;
builder.Configuration[$"{BaseModule.ApplicationSection}:{nameof(OnboardingHostOptions.OnboardingUrl)}"] =
    new UriBuilder(webOrigin) { Host = $"{onboardingSubdomain}.{tenantBaseDomain}" }.Uri.ToString().TrimEnd('/');
builder.Configuration[$"{BaseModule.ApplicationSection}:{nameof(SignInHostOptions.SignInUrl)}"] =
    new UriBuilder(webOrigin) { Host = $"{signInSubdomain}.{tenantBaseDomain}" }.Uri.ToString().TrimEnd('/');
builder.Configuration["Vitarish:Security:MandatoryAuditEnabled"] = "true";
builder.Configuration["Vitarish:Security:Audit:CapacityWarningBytes"] = "1073741824";
builder.Configuration["Vitarish:Security:AuthenticationAttempts:Realm"] = apiOrigin.Host;
builder.Configuration["Vitarish:Security:AuthenticationAttempts:CurrentPepper"] = "e2e-authentication-attempt-key-pepper-20260829";
builder.Configuration["Vitarish:Security:AuthenticationAttempts:CurrentVersion"] = "v1";
builder.Configuration[$"{FileTransferAuditKeyOptions.SectionName}:{nameof(FileTransferAuditKeyOptions.CurrentKey)}"] = "e2e-file-transfer-audit-key-20261026";
builder.Configuration["Vitarish:Security:Tls:RequireHttps"] = "true";
builder.Configuration["Vitarish:Security:TokenValidation:Issuer"] = apiOrigin.ToString().TrimEnd('/');
builder.Configuration["Vitarish:Security:TokenValidation:Audiences:0"] = "vitarish-api";
SecurityConfigurationOptions securityConfiguration = SecurityConfigurationValidator.Validate(builder.Configuration, builder.Environment);

Dictionary<Type, string> databaseNames = [];
E2eAuditStoreOutageSimulation auditStoreOutage = new();
void configureInMemory(DbContextOptionsBuilder options)
{
    Type contextType = options.Options.ContextType;
    if (!databaseNames.TryGetValue(contextType, out string? databaseName))
    {
        databaseName = $"vitarish-e2e-{contextType.FullName}-{Guid.NewGuid():N}";
        databaseNames[contextType] = databaseName;
    }
    options.UseInMemoryDatabase(databaseName);
    if (contextType == typeof(AuditDbContext))
        options.AddInterceptors(auditStoreOutage);
    // The in-memory store has no transactions; workflows that open one (e.g. reminder replacement) run without it here.
    options.ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
}

await builder.RegisterHosting(new HostingCompositionOptions(
    configureInMemory,
    EphemeralCertificateFactory.Create(apiOrigin.Host),
    securityConfiguration));

_ = builder.Services.AddSingleton(
    typeof(IOperationReceiptStore<>),
    typeof(E2eInMemoryOperationReceiptStore<>));
builder.Services.Replace(ServiceDescriptor.Singleton<IObjectStorageService, E2eObjectStorageService>());
builder.Services.Replace(ServiceDescriptor.Singleton<ISmsProvider>(_ => new E2eSmsCapture(smsCapturePath)));
_ = builder.Services.AddSingleton<E2eAuditCapacitySimulation>();
builder.Services.Replace(ServiceDescriptor.Transient<IAuditStorageSizeReader, E2eAuditStorageSizeReader>());
_ = builder.Services.AddAuditCapacityAlertMonitoring(builder.Environment, openApiGeneration: false);
_ = builder.Services.AddSingleton(auditStoreOutage);
_ = builder.Services.AddSingleton<IAuditStorageOutageCounter, E2eAuditStorageOutageCounter>();
_ = builder.Services.AddAuditStorageOutageReporting(openApiGeneration: false);
builder.Services.AddSingleton<Hangfire.IBackgroundJobClient, E2eBackgroundJobClient>();
builder.Services.Replace(ServiceDescriptor.Singleton<IClinicalEventProposalStore, E2eClinicalEventProposalStore>());
builder.Services.Replace(ServiceDescriptor.Singleton<IAssistantActionStore, E2eAssistantActionStore>());
builder.Services.Replace(ServiceDescriptor.Singleton<ISurgeryAssistantProposalStore, E2eSurgeryAssistantProposalStore>());

ServiceDescriptor? productionSurgeryViewRefresher = builder.Services.SingleOrDefault(descriptor =>
    descriptor.ServiceType == typeof(IProjectObjectMaterializedViewRefresher)
    && descriptor.ImplementationType?.Name == surgeryMaterializedViewRefresherTypeName);
if (productionSurgeryViewRefresher is not null)
    _ = builder.Services.Remove(productionSurgeryViewRefresher);
_ = builder.Services.AddTransient<IProjectObjectMaterializedViewRefresher, E2eSurgeryViewRefresher>();

builder.Services.Replace(ServiceDescriptor.Transient<IRequestHandler<PatientViewRefreshCommand>, E2ePatientViewRefresher>());

_ = builder.Services.AddScoped<ClinicDbContext>(services =>
    new E2eClinicDbContext(
        services.GetRequiredService<DbContextOptions<ClinicDbContext>>(),
        services.GetRequiredService<Vitarish.Requests.Application.Abstractions.IRequestContext>(),
        services.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()));

_ = builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider().SetApplicationName(apiOrigin.Host);
_ = builder.Services.AddDistributedMemoryCache();
builder.Services.Replace(ServiceDescriptor.Singleton<IAuthenticationChallengeStore, InMemoryAuthenticationChallengeStore>());
builder.Services.Replace(ServiceDescriptor.Singleton<IAuthenticationAttemptTracker, InMemoryAuthenticationAttemptTracker>());
_ = builder.Services.AddOpenApi();
WebApplication app = builder.Build();
_ = app.UseHostingPipeline(securityConfiguration);
_ = app.MapModuleEndpoints();
_ = app.MapSecurityAuditApi();
_ = app.MapAppEndpoints();
_ = app.MapE2eAuditEvidenceEndpoints();

await E2eFixtureSeeder.SeedAsync(app.Services, password, CancellationToken.None);
// Main initializes every app; the E2E host initializes only the landing app, which provisions the
// landing tenant at its own host so landing sign-in can be tested.
await app.Services.GetRequiredService<IReadOnlyList<Vitarish.Apps.Base.Core.IApp>>().OfType<Vitarish.Landing.App.LandingApp>().Single().Initialize(app);
app.Run();

static Uri RequireLoopbackOrigin(string? value, string name, bool requireHttps)
{
    if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? origin) || (!origin.IsLoopback && !origin.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)) || origin.AbsolutePath != "/" || origin.Query.Length != 0 || origin.Fragment.Length != 0)
        throw new InvalidOperationException($"{name} must be an absolute loopback origin with an explicit port.");
    if (requireHttps && origin.Scheme != Uri.UriSchemeHttps)
        throw new InvalidOperationException($"{name} must use HTTPS.");
    if (!requireHttps && origin.Scheme != Uri.UriSchemeHttp && origin.Scheme != Uri.UriSchemeHttps)
        throw new InvalidOperationException($"{name} must use HTTP or HTTPS.");
    return origin;
}
