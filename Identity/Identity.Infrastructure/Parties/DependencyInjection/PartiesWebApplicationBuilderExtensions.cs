using DnsClient;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Base.Infrastructure.Pipeline;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Core.Parties.Application.Services;
using Vitarish.Identity.Infrastructure.Parties.Abstractions;
using Vitarish.Identity.Infrastructure.Parties.Data;
using Vitarish.Identity.Infrastructure.Parties.Handlers;
using Vitarish.Identity.Infrastructure.Parties.Services;

namespace Vitarish.Identity.Infrastructure.Parties.DependencyInjection;

/// <summary>
/// Service registration helpers for parties features.
/// </summary>
public static class PartiesWebApplicationBuilderExtensions
{
    public static void AddIdentityParties(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddDbContext<PartiesDbContext>(dbContextOptionsBuilder);

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateTenantHandler).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(CreateOwner));

        _ = builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        _ = builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        _ = builder.Services.AddTransient<IPartiesService, PartiesService>();
        _ = builder.Services.AddScoped<ITenantsService, TenantsService>();

        _ = builder.Services.AddSingleton<ILookupClient>(_ =>
        {
            var lookupClientOptions = new LookupClientOptions
            {
                Timeout = TimeSpan.FromSeconds(5),
                UseCache = true,
            };

            return new LookupClient(lookupClientOptions);
        });

        _ = builder.Services.AddSingleton<IDnsVerificationService, DnsVerificationService>();
    }
}
