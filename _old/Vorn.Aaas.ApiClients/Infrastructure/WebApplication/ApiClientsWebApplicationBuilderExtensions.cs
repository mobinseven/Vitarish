using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.ApiClients.Application.Commands;
using Vorn.Aaas.ApiClients.Infrastructure.Data;
using Vorn.Aaas.ApiClients.Presentation.Services;

namespace Vorn.Aaas.ApiClients.Infrastructure.WebApplication;

/// <summary>
/// Service registration helpers for API client features.
/// </summary>
public static class ApiClientsWebApplicationBuilderExtensions
{
    public static void AddAaasApiClients(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        builder.Services.AddDbContext<ApiClientDbContext>(dbContextOptionsBuilder);

        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateApiClient).Assembly));
        builder.Services.AddValidatorsFromAssemblyContaining(typeof(CreateApiClient));

        builder.Services.AddTransient<IApiClientsService, ApiClientsService>();

    }
}
