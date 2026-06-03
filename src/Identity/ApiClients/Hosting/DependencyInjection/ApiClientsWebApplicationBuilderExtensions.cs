using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.ApiClients.Application.Abstractions;
using Vitarish.ApiClients.Application.Commands;
using Vitarish.ApiClients.Infrastructure;
using Vitarish.ApiClients.Infrastructure.Data;
using Vitarish.ApiClients.Infrastructure.Services;
using Vitarish.Entities.Server.Extensions;

namespace Vitarish.ApiClients.Hosting.DependencyInjection;

/// <summary>
/// Service registration helpers for API client features.
/// </summary>
public static class ApiClientsWebApplicationBuilderExtensions
{
    public static void AddAaasApiClients(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddTransientDbContext<ApiClientDbContext>((_, options) => dbContextOptionsBuilder(options));

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(AssemblyMarker));

        _ = builder.Services.AddTransient<IApiClientsService, ApiClientsService>();

    }
}
