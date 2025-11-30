using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.AuthenticationClients.Application.Commands;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Services;
using Vorn.Aaas.AuthenticationClients.Presentation.Abstractions;

/// <summary>
/// Service registration helpers for authentication client features.
/// </summary>
public static class AuthenticationClientsWebApplicationBuilderExtensions
{
    public static void AddAaasAuthenticationClients(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddDbContext<AuthenticationClientDbContext>(dbContextOptionsBuilder);

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateAuthenticationClient).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(CreateAuthenticationClient));

        _ = builder.Services.AddTransient<IAuthenticationClientsService, AuthenticationClientsService>();
    }
}
