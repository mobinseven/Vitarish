using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Requests.Application;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Infrastructure;
using Vitarish.Requests.Infrastructure.Data;
using Vitarish.Requests.Infrastructure.Services;

namespace Vitarish.Requests.Hosting.DependencyInjection;

public static class IdentityRequestsExtensions
{
    /// <summary>
    /// Adds request identities services including the server-side <see cref="IRequestContext"/>.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="dbContextOptionsBuilder">Configuration for the database context.</param>
    public static void AddRequests(
        this WebApplicationBuilder builder,
        Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddDbContext<RequestsDbContext>(dbContextOptionsBuilder, ServiceLifetime.Transient);
        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(AssemblyMarker));
        _ = builder.Services.AddTransient<IRequestIdentitiesService, RequestIdentitiesService>();

        // Register the server-side request identity context
        builder.Services.AddRequestsContext(builder.Configuration);
    }

    /// <summary>
    /// Adds the server-side <see cref="IRequestContext"/> service that is populated from HttpContext.User claims.
    /// Also registers <see cref="IRequestContextWriter"/> for non-HTTP scenarios.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration for options binding.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRequestsContext(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind options if configuration section exists
        IConfigurationSection section = configuration.GetSection(RequestsOptions.Section);
        if (section.Exists())
        {
            _ = services.Configure<RequestsOptions>(section);
        }

        _ = services.AddHttpContextAccessor();

        // Register the context as scoped - one per request
        _ = services.AddScoped<RequestContext>();
        _ = services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<RequestContext>());
        _ = services.AddScoped<IRequestContextWriter>(sp => sp.GetRequiredService<RequestContext>());

        return services;
    }
}
