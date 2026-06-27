using DnsClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Parties.Application.Abstractions;
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

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));

        // Register Entity Services
        _ = builder.Services.AddTransient<OwnersService>();
        _ = builder.Services.AddTransient<TenantsService>();
        _ = builder.Services.AddTransient<TenantIndicatorService>();

        _ = builder.Services.AddTransient<ITenantResolver, TenantResolver>();
        _ = builder.Services.AddTransient<IPartiesService, PartiesService>();
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
