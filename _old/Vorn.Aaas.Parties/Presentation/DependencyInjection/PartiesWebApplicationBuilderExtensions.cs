using DnsClient;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.Parties.Application.Commands;
using Vorn.Aaas.Parties.Application.Services;
using Vorn.Aaas.Parties.Infrastructure.Abstractions;
using Vorn.Aaas.Parties.Infrastructure.Data;
using Vorn.Aaas.Parties.Infrastructure.Services;
using Vorn.Aaas.Parties.Presentation.Abstractions;

namespace Vorn.Aaas.Parties.Presentation.DependencyInjection;

/// <summary>
/// Service registration helpers for parties features.
/// </summary>
public static class PartiesWebApplicationBuilderExtensions
{
    public static void AddAaasParties(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddDbContext<PartiesDbContext>(dbContextOptionsBuilder);

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateOwner).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(CreateOwner));

        _ = builder.Services.AddTransient<IPartiesService, PartiesService>();
        _ = builder.Services.AddScoped<ITenantsService, TenantsService>();
        _ = builder.Services.AddSingleton<ILookupClient>(sp =>
        {
            var lookupClientOptions = new LookupClientOptions
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
