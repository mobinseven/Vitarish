using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Infrastructure.DependencyInjection;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Contracts.Authorization;

namespace Vitarish.Authentication.Module.DependencyInjection;

public static class AuthenticationModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationModuleServices(this IServiceCollection services, AuthenticationProfilePolicy profilePolicy, Action<DbContextOptionsBuilder>? configureDbContext = null)
    {
        services.AddSingleton(profilePolicy);
        services.AddScoped<LoginWithPasswordHandler>();
        services.AddScoped<ChangePasswordHandler>();
        services.AddScoped<BeginPhoneLoginHandler>();
        services.AddScoped<CompletePhoneLoginHandler>();
        services.AddScoped<BeginEmailLoginHandler>();
        services.AddScoped<CompleteEmailLoginHandler>();
        services.AddScoped<BeginPasskeyLoginHandler>();
        services.AddScoped<CompletePasskeyLoginHandler>();
        services.AddScoped<CompletePasswordLoginOtpHandler>();
        services.AddScoped<AuthenticationOtpDeliveryService>();
        services.AddScoped<AuthenticationAttemptAuditor>();
        services.AddScoped<BeginPasswordRecoveryHandler>();
        services.AddScoped<CompletePasswordRecoveryHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<GetCurrentAuthenticationSessionHandler>();
        services.AddScoped<GetLogoutContextHandler>();
        services.AddScoped<GetSecurityAccessPolicyHandler>();
        services.AddScoped<ChangeSecurityAccessPolicyHandler>();
        services.AddScoped<GetAuthenticationAccessHistoryHandler>();
        services.AddScoped<GetAuthenticationSessionNoticesHandler>();
        services.AddScoped<AcknowledgeAuthenticationSessionNoticeHandler>();
        services.AddScoped<AcknowledgeAuthenticationAccessHistoryHandler>();
        services.AddScoped<ChangeUserInactivityTimeoutHandler>();
        _ = services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthenticationAuthorizationPolicies.ReadSessionPolicy, policy => policy.RequireRole(IdentityDefinitions.PlatformAdmin));
            options.AddPolicy(AuthenticationAuthorizationPolicies.ChangeSessionPolicy, policy => policy.RequireRole(IdentityDefinitions.PlatformAdmin));
            options.AddPolicy(AuthenticationAuthorizationPolicies.ChangeUserInactivityPolicy, policy => policy.RequireRole(IdentityDefinitions.PlatformAdmin));
        });
        services.AddHttpContextAccessor();
        services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
        return services.AddAuthenticationInfrastructure(configureDbContext);
    }
}
