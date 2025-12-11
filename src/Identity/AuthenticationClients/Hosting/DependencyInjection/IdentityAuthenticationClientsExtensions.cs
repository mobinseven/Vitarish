using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.AuthenticationClients.Application.Abstractions;
using Vitarish.AuthenticationClients.Application.Commands;
using Vitarish.AuthenticationClients.Infrastructure;
using Vitarish.AuthenticationClients.Infrastructure.Data;
using Vitarish.AuthenticationClients.Infrastructure.Services;

/// <summary>
/// Service registration helpers for authentication client features.
/// </summary>
public static class IdentityAuthenticationClientsExtensions
{
    public static void AddAaasAuthenticationClients(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddDbContext<AuthenticationClientDbContext>(dbContextOptionsBuilder);

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyMarker).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(AssemblyMarker));

        _ = builder.Services.AddTransient<IAuthenticationClientsService, AuthenticationClientsService>();
    }
}
