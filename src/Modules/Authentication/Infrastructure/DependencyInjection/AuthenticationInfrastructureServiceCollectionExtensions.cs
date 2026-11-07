using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.ReturnUrls;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Authentication.Infrastructure.Identity;
using Vitarish.Authentication.Infrastructure.Time;
using Vitarish.Entities.Server.Extensions;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure.DependencyInjection;

public static class AuthenticationInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationInfrastructure(this IServiceCollection services, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        _ = services.AddTransientDbContext<AuthenticationDbContext>((_, options) => configureDbContext?.Invoke(options));
        _ = services.AddScoped<SecurityAccessPolicyStore>();
        _ = services.AddScoped<IAuthenticationSecurityPolicyStore>(provider => provider.GetRequiredService<SecurityAccessPolicyStore>());
        _ = services.AddScoped<IAuthenticationSecurityPolicyProvider>(provider => provider.GetRequiredService<SecurityAccessPolicyStore>());
        _ = services.AddScoped<IEnabledAuthenticationMethods, EnabledAuthenticationMethodsReader>();
        _ = services.AddScoped<IPersonalCredentialHost, PersonalCredentialHost>();
        _ = services.AddOptions<AuthenticationAttemptKeyOptions>()
            .BindConfiguration(AuthenticationAttemptKeyOptions.SectionName)
            .ValidateOnStart();
        _ = services.AddSingleton<IValidateOptions<AuthenticationAttemptKeyOptions>>(provider =>
            new AuthenticationAttemptKeyOptionsValidator(
                provider.GetRequiredService<IConfiguration>(),
                provider.GetService<IHostEnvironment>()));
        _ = services.AddOptions<DeliveredCodeLogOptions>()
            .BindConfiguration(DeliveredCodeLogOptions.SectionName);
        _ = services.AddSingleton<IAuthenticationAttemptKeyFactory, AuthenticationAttemptKeyFactory>();
        _ = services.AddSingleton<IAuthenticationAttemptTracker, RedisAuthenticationAttemptTracker>();
        _ = services.AddScoped<IPasswordValidator<User>, SecurityPolicyPasswordValidator>();
        _ = services.AddScoped<UnknownPasswordVerifier>();
        services.AddHttpContextAccessor();
        services.TryAddScoped<IClientIpAddressAccessor, UnavailableClientIpAddressAccessor>();
        services.AddSingleton<AuthenticationChallengePolicy>(AuthenticationChallengePolicy.Default);
        services.AddSingleton<IAuthenticationChallengeStore, DistributedAuthenticationChallengeStore>();
        services.AddScoped<IAuthenticationUserDirectory, AspNetIdentityAuthenticationUserDirectory>();
        services.AddTransient<Microsoft.AspNetCore.Authentication.IClaimsTransformation, LiveAccountClaimsTransformation>();
        services.AddScoped<IInitialPlatformAdministratorGrant, UsersInitialPlatformAdministratorGrant>();
        services.AddScoped<IAuthenticationSignInService, AspNetIdentitySignInService>();
        services.AddScoped<ITenantHandoffTicketStore, TenantHandoffTicketStore>();
        services.AddScoped<AuthenticationSessionRegistry>();
        services.AddScoped<AuthenticationSessionLinkContext>();
        services.AddScoped<IAuthenticationContinuationStore, AuthenticationContinuationStore>();
        services.AddScoped<IAuthenticationBrowserCorrelation, AuthenticationBrowserCorrelation>();
        services.AddScoped<IPendingAuthenticationChallenge, PendingAuthenticationChallengeCookie>();
        services.AddScoped<IAuthenticationBrowserSessionDirectory, AuthenticationBrowserSessionDirectory>();
        services.AddScoped<IAuthenticationSessionRootReader, AuthenticationSessionRootReader>();
        services.AddScoped<IAuthenticationPreviousSessionCookie, AuthenticationPreviousSessionCookie>();
        services.AddSingleton<IAuthenticationCentralDestinationPolicy, AuthenticationCentralDestinationPolicy>();
        services.AddScoped<IAuthenticationSessionRegistry>(provider => provider.GetRequiredService<AuthenticationSessionRegistry>());
        services.AddScoped<IAuthenticationActiveSessionQuery>(provider => provider.GetRequiredService<AuthenticationSessionRegistry>());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IAuthenticationUtcClock, AuthenticationUtcClock>();
        services.AddScoped<AuthenticationCookieEvents>();
        services.AddScoped<IAuthenticationCodeVerifier, AspNetIdentityCodeVerifier>();
        services.AddScoped<IAuthenticationCodeGenerator, AspNetIdentityCodeGenerator>();
        services.AddScoped<IAuthenticationReturnUrlStore, CookieAuthenticationReturnUrlStore>();
        services.AddScoped<IAuthenticationRedirectResolver, JourneyAwareRedirectResolver>();
        services.AddScoped<IPhoneAuthenticationCodeSender, SmsAuthenticationCodeSender>();
        services.AddScoped<IEmailAuthenticationCodeSender, EmailAuthenticationCodeSender>();
        services.AddScoped<IAuthenticationRecoveryDeliveryFailureRecorder, AuthenticationRecoveryDeliveryFailureRecorder>();
        services.AddSingleton<IPasswordRecoveryCodeDispatcher, PasswordRecoveryCodeDispatcher>();
        services.AddScoped<IWebAuthnAuthenticationService, WebAuthnAuthenticationService>();
        services.AddScoped<ILogoutContextService, DuendeLogoutContextService>();
        services.AddSingleton<VerificationSmsRequestShield, InMemoryVerificationSmsRequestShield>();
        services.AddScoped<IUserAccountUpdater, UserAccountUpdater>();
        return services;
    }
}
