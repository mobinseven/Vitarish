using DnsClient;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Commands;
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
        _ = builder.Services.AddDbContext<PartiesDbContext>(dbContextOptionsBuilder, ServiceLifetime.Transient);

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining<CreateTenantValidator>();

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
