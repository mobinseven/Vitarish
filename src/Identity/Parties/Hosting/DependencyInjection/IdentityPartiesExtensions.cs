using DnsClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Infrastructure;
using Vitarish.Parties.Infrastructure.Abstractions;
using Vitarish.Parties.Infrastructure.Data;
using Vitarish.Parties.Infrastructure.Services;

namespace Vitarish.Parties.Hosting.DependencyInjection;

/// <summary>
/// Service registration helpers for parties features.
/// </summary>
public static class IdentityPartiesExtensions
{
    public static void AddParties(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        builder.Services.AddVitarishPartiesDomainEntityHandlers();
        _ = builder.Services.AddVitarishPartiesInfrastructure();
        _ = builder.Services.AddDbContext<PartiesDbContext>(dbContextOptionsBuilder, ServiceLifetime.Transient);

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));

        // Register Entity Services
        _ = builder.Services.AddTransient<OwnerService>();
        _ = builder.Services.AddTransient<TenantService>();
        _ = builder.Services.AddTransient<TenantIndicatorService>();

        _ = builder.Services.AddTransient<ITenantResolver, TenantResolver>();
        _ = builder.Services.AddTransient<IPartiesService, PartiesService>();
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
