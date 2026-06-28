using Microsoft.Extensions.DependencyInjection;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Infrastructure;

namespace Vitarish.Authentication.Infrastructure.DependencyInjection;

public static class AuthenticationInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAuthenticationChallengeStore, InMemoryAuthenticationChallengeStore>();
        services.AddScoped<IAuthenticationUserDirectory, AspNetIdentityAuthenticationUserDirectory>();
        services.AddScoped<IAuthenticationSignInService, AspNetIdentitySignInService>();
        services.AddScoped<IAuthenticationCodeVerifier, AspNetIdentityCodeVerifier>();
        services.AddScoped<IAuthenticationReturnUrlStore, CookieAuthenticationReturnUrlStore>();
        services.AddScoped<IAuthenticationRedirectResolver, CookieAuthenticationReturnUrlStore>();
        services.AddScoped<IAuthenticationCodeSender, EmailAuthenticationCodeSender>();
        services.AddScoped<IWebAuthnAuthenticationService, WebAuthnAuthenticationService>();
        services.AddScoped<ILogoutContextService, DuendeLogoutContextService>();
        services.AddSingleton<VerificationSmsRequestShield, InMemoryVerificationSmsRequestShield>();
        return services;
    }
}
