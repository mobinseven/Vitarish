using Microsoft.Extensions.DependencyInjection;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.ReturnUrls;

namespace Vitarish.Authentication.Infrastructure.DependencyInjection;

public static class AuthenticationInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton<AuthenticationChallengePolicy>(AuthenticationChallengePolicy.Default);
        services.AddSingleton<IAuthenticationChallengeStore, DistributedAuthenticationChallengeStore>();
        services.AddScoped<IAuthenticationUserDirectory, AspNetIdentityAuthenticationUserDirectory>();
        services.AddScoped<IAuthenticationSignInService, AspNetIdentitySignInService>();
        services.AddScoped<IAuthenticationCodeVerifier, AspNetIdentityCodeVerifier>();
        services.AddScoped<IAuthenticationCodeGenerator, AspNetIdentityCodeGenerator>();
        services.AddScoped<IAuthenticationReturnUrlStore, CookieAuthenticationReturnUrlStore>();
        services.AddScoped<IAuthenticationRedirectResolver, JourneyAwareRedirectResolver>();
        services.AddScoped<IPhoneAuthenticationCodeSender, SmsAuthenticationCodeSender>();
        services.AddScoped<IEmailAuthenticationCodeSender, EmailAuthenticationCodeSender>();
        services.AddScoped<IWebAuthnAuthenticationService, WebAuthnAuthenticationService>();
        services.AddScoped<ILogoutContextService, DuendeLogoutContextService>();
        services.AddSingleton<VerificationSmsRequestShield, InMemoryVerificationSmsRequestShield>();
        services.AddScoped<IUserAccountUpdater, UserAccountUpdater>();
        return services;
    }
}
