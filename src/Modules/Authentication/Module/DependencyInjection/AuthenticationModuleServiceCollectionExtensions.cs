using Microsoft.Extensions.DependencyInjection;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Infrastructure.DependencyInjection;

namespace Vitarish.Authentication.Module.DependencyInjection;

public static class AuthenticationModuleServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationModuleServices(this IServiceCollection services)
    {
        services.AddScoped<LoginWithPasswordHandler>(); services.AddScoped<BeginPhoneLoginHandler>(); services.AddScoped<CompletePhoneLoginHandler>(); services.AddScoped<BeginEmailLoginHandler>(); services.AddScoped<CompleteEmailLoginHandler>(); services.AddScoped<BeginPasskeyLoginHandler>(); services.AddScoped<CompletePasskeyLoginHandler>(); services.AddScoped<LogoutHandler>(); services.AddScoped<GetCurrentAuthenticationSessionHandler>(); services.AddScoped<GetLogoutContextHandler>();
        services.AddHttpContextAccessor(); services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
        return services.AddAuthenticationInfrastructure();
    }
}
