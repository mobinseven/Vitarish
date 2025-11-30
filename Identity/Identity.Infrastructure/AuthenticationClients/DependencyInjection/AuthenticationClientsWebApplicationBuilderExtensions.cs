using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Identity.Core.AuthenticationClients.Application.Commands;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Data;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Handlers;
using Vitarish.Identity.Infrastructure.AuthenticationClients.Services;

namespace Vitarish.Identity.Infrastructure.AuthenticationClients.DependencyInjection;

/// <summary>
/// Service registration helpers for authentication client features.
/// </summary>
public static class AuthenticationClientsWebApplicationBuilderExtensions
{
    public static void AddIdentityAuthenticationClients(this WebApplicationBuilder builder, Action<DbContextOptionsBuilder> dbContextOptionsBuilder)
    {
        _ = builder.Services.AddDbContext<AuthenticationClientDbContext>(dbContextOptionsBuilder);

        _ = builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateAuthenticationClientHandler).Assembly));
        _ = builder.Services.AddValidatorsFromAssemblyContaining(typeof(CreateAuthenticationClient));

        _ = builder.Services.AddTransient<IAuthenticationClientsService, AuthenticationClientsService>();
    }
}
