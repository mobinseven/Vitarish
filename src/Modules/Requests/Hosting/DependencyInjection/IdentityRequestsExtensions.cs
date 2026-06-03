using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Requests.Application;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Infrastructure;
using Vitarish.Requests.Infrastructure.Data;
using Vitarish.Requests.Infrastructure.Services;
using Vitarish.Entities.Server.Extensions;

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
        _ = builder.Services.AddTransientDbContext<RequestsDbContext>((_, options) => dbContextOptionsBuilder(options));
        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(AssemblyMarker));
        _ = builder.Services.AddTransient<IRequestIdentitiesService, RequestIdentitiesService>();

        IConfigurationSection section = builder.Configuration.GetSection(RequestsOptions.Section);
        if (section.Exists())
        {
            _ = builder.Services.Configure<RequestsOptions>(section);
        }

        _ = builder.Services.AddHttpContextAccessor();
        _ = builder.Services.AddScoped<RequestContext>();
        _ = builder.Services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<RequestContext>());
        _ = builder.Services.AddScoped<IRequestContextWriter>(sp => sp.GetRequiredService<RequestContext>());
        _ = builder.Services.AddScoped<IInterceptor, RequestContextInterceptor>();
    }
}
