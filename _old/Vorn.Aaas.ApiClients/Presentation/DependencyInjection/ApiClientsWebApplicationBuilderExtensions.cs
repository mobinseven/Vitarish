using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.ApiClients.Application.Commands;
using Vorn.Aaas.ApiClients.Infrastructure.Data;
using Vorn.Aaas.ApiClients.Infrastructure.Services;
using Vorn.Aaas.ApiClients.Presentation.Abstractions;

namespace Vorn.Aaas.ApiClients.Presentation.DependencyInjection;

/// <summary>
/// Service registration helpers for API client features.
/// </summary>
public static class ApiClientsWebApplicationBuilderExtensions
{
    public static void AddAaasApiClients(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddDbContext<ApiClientDbContext>(dbContextOptionsBuilder);

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateApiClient).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(CreateApiClient));

        _ = builder.Services.AddTransient<IApiClientsService, ApiClientsService>();

    }
}
