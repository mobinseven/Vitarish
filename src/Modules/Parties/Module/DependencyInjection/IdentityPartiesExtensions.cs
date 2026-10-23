using System.Threading.RateLimiting;
using DnsClient;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Parties.Application;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Addresses;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Application.SignIn;
using Vitarish.Parties.Api;
using Vitarish.Parties.Infrastructure;
using Vitarish.Parties.Infrastructure.Abstractions;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Parties.Module.BackgroundJobs;
using Vitarish.Modules.Base.Core.BackgroundJobs;

namespace Vitarish.Parties.Module.DependencyInjection;

/// <summary>
/// Service registration helpers for parties features.
/// </summary>
public static class IdentityPartiesExtensions
{
    public static void AddParties(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        builder.Services.AddVitarishPartiesDomainEntityHandlers();
        _ = builder.Services.AddVitarishPartiesInfrastructure();
        _ = builder.Services.AddTransientDbContext<PartiesDbContext>((_, options) => dbContextOptionsBuilder(options));

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(typeof(AssemblyMarker).Assembly, typeof(PlatformAdminTenant).Assembly));
        _ = builder.Services.AddOptions<PlatformAdminHostOptions>().BindConfiguration(Modules.Base.Core.Module.ApplicationSection);
        _ = builder.Services.AddOptions<TenantAddressOptions>().BindConfiguration(Modules.Base.Core.Module.ApplicationSection);
        _ = builder.Services.AddOptions<OnboardingHostOptions>().BindConfiguration(Modules.Base.Core.Module.ApplicationSection);
        _ = builder.Services.AddOptions<SignInHostOptions>().BindConfiguration(Modules.Base.Core.Module.ApplicationSection);
        _ = builder.Services.AddRateLimiter(options => options.AddPolicy(PartiesRateLimitPolicies.SelfServiceTenantCreation, context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new SlidingWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromHours(1), PermitLimit = 10, SegmentsPerWindow = 6,
                    QueueLimit = 0, QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                })));

        // Register Entity Services
        _ = builder.Services.AddTransient<OwnersService>();
        _ = builder.Services.AddTransient<TenantsService>();
        _ = builder.Services.AddTransient<TenantIndicatorService>();

        _ = builder.Services.AddTransient<ITenantResolver, TenantResolver>();
        _ = builder.Services.AddTransient<ITenantHostnameRegistry, TenantHostnameRegistry>();
        _ = builder.Services.AddTransient<ITenantOriginTrust, TenantOriginTrust>();
        _ = builder.Services.AddTransient<ITenantCreatorDirectory, UsersTenantCreatorDirectory>();
        _ = builder.Services.AddTransient<Vitarish.Parties.Application.Onboarding.IOnboardingSetupDraftStore, ProfileOnboardingSetupDraftStore>();
        _ = builder.Services.AddTransient<ISelfServiceTenantStore, SelfServiceTenantStore>();
        _ = builder.Services.AddTransient<ITenantHandoffIssuer, AuthenticationTenantHandoffIssuer>();
        _ = builder.Services.AddTransient<Vitarish.Authentication.Application.Abstractions.IRoleFreeSessionPolicy, PlatformHostRoleFreeSessionPolicy>();
        _ = builder.Services.AddTransient<ISignInClinicDirectory, SignInClinicDirectory>();
        _ = builder.Services.AddTransient<IAppTenantDirectory, AppTenantDirectory>();
        _ = builder.Services.AddTransient<ITenantBusinessProfileStore, TenantBusinessProfileStore>();
        _ = builder.Services.AddTransient<ITenantImageFiles, FilesTenantImageFiles>();
        _ = builder.Services.AddTransient<IAppTenantProvisioner, EfAppTenantProvisioner>();
        _ = builder.Services.AddTransient<IPartiesService, PartiesService>();
        _ = builder.Services.AddTransient<Vitarish.Users.Application.Abstractions.ITenantExistence, TenantExistence>();
        _ = builder.Services.AddTransient<IBackgroundTenantSource, PartiesBackgroundTenantSource>();
        _ = builder.Services.AddSingleton<ILookupClient>(sp =>
        {
            LookupClientOptions lookupClientOptions = new()
            {
                Timeout = TimeSpan.FromSeconds(5),
                UseCache = true,
                // Configure other options as needed
            };
            return new LookupClient(lookupClientOptions);
        });
        _ = builder.Services.AddSingleton<IDnsVerificationService, DnsVerificationService>();
    }
}
