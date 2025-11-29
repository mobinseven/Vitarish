using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vorn.Aaas.AuthenticationClients.Application.Commands;
using Vorn.Aaas.AuthenticationClients.Infrastructure.Data;
using Vorn.Aaas.AuthenticationClients.Presentation.Services;

/// <summary>
/// Service registration helpers for authentication client features.
/// </summary>
public static class AuthenticationClientsWebApplicationBuilderExtensions
{
    public static void AddAaasAuthenticationClients(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        builder.Services.AddDbContext<AuthenticationClientDbContext>(dbContextOptionsBuilder);

        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateAuthenticationClient).Assembly));
        builder.Services.AddValidatorsFromAssemblyContaining(typeof(CreateAuthenticationClient));

        builder.Services.AddTransient<IAuthenticationClientsService, AuthenticationClientsService>();
    }
}
