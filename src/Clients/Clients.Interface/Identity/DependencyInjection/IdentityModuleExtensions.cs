using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Vitarish.Clients.Identity.Options;
using Vitarish.Clients.Interface.Identity.Constants;
using Vitarish.Clients.Interface.Identity.Extensions;
using Vitarish.Clients.Interface.Identity.Services;

public static class IdentityModuleExtensions
{
    private const string ConfigSection = "Vitarish";
    public const string DefaultChallengeScheme = "oidc";

    public static IdentityClientOptions GetIdentityOptions(this IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection($"{ConfigSection}:Identity");
        IdentityClientOptions options = section.Get<IdentityClientOptions>() ?? throw new InvalidOperationException($"Configuration section '{ConfigSection}:Identity' is missing or invalid.");
        return options;
    }
    public static async Task<IServiceCollection> AddIdentityComponents(this IServiceCollection services, IConfiguration configuration)
    {
        if (services.Any(sd => sd.ServiceType == typeof(IUsersService)))
        {
            return services;
        }
        IdentityClientOptions aaasOptions = configuration.GetIdentityOptions();

        _ = services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = DefaultChallengeScheme;
            options.DefaultSignOutScheme = DefaultChallengeScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromDays(aaasOptions.CookieExpirationDays);
            o.Cookie.MaxAge = TimeSpan.FromDays(aaasOptions.CookieExpirationDays);
            o.SlidingExpiration = true;
        })
        .AddOpenIdConnect(DefaultChallengeScheme, options =>
        {
            options.Authority = aaasOptions.Authority;
            options.ClientId = aaasOptions.ClientId;
            options.MapInboundClaims = false;
            options.GetClaimsFromUserInfoEndpoint = true;
            options.SaveTokens = true;
            options.NonceCookie.Expiration = TimeSpan.FromMinutes(30);
            options.Events.OnSignedOutCallbackRedirect += context =>
            {
                context.Response.Redirect(context.Options.SignedOutRedirectUri);
                context.HandleResponse();
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToIdentityProviderForSignOut = context =>
            {
                string? idToken = context.Properties.GetTokenValue("id_token");
                if (!string.IsNullOrEmpty(idToken))
                {
                    context.ProtocolMessage.IdTokenHint = idToken;   // avoid "Are you sure?" page
                }
                return Task.CompletedTask;
            };
            options.Events.OnAuthenticationFailed = context =>
            {
                _ = context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                ILogger logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("OIDC");
                logger.LogError(context.Exception, "OIDC authentication failed — redirecting to login.");

                context.Response.Redirect("authentication/login?error=auth_failed");
                context.HandleResponse();
                return Task.CompletedTask;
            };
            options.Events.OnRemoteFailure = context =>
            {
                bool isCorrelationFailure =
                    context.Failure?.Message?.Contains("Correlation failed", StringComparison.OrdinalIgnoreCase) == true;

                if (isCorrelationFailure)
                {
                    _ = context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    ILogger logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("OIDC");
                    logger.LogError(context.Failure, "OIDC correlation validation failed — user will be redirected to error page.");

                    string loginUrl = "authentication/login?error=correlation_failure";
                    context.Response.Redirect(loginUrl);
                    context.HandleResponse();
                    return Task.CompletedTask;
                }

                return Task.FromException(context.Failure!);
            };
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(30)
            };
        });
        if (!services.Any(sd => sd.ServiceType == typeof(IDistributedCache)))
        {
            _ = services.AddDistributedMemoryCache();
        }
        _ = services.AddMemoryCache();
        _ = services.AddAuthorization();
        _ = services.ConfigureCookieOidcRefresh(CookieAuthenticationDefaults.AuthenticationScheme, DefaultChallengeScheme);
        _ = services.AddCascadingAuthenticationState();
        _ = services.AddHttpContextAccessor();
        _ = services.AddHttpClient();
        _ = services.AddSingleton<IAuthorizationMiddlewareResultHandler, BlazorAuthorizationMiddlewareResultHandler>();
        _ = services.AddIdentityClient(configuration);
        await services.AddIdentityClientDataProtection(configuration);
        _ = services.AddScoped<IPersonalAccessTokenClient, PersonalAccessTokenClient>();
        _ = services.AddScoped<IPersonalClientFactory, PersonalClientFactory>();
        _ = services.AddScoped<IUsersService, UsersService>();
        _ = services.AddScoped<UserProfileClient>();
        _ = services.AddScoped<UserProfileCultureInfoProvider>();

        return services;
    }
    public static WebApplication MapIdentityModule(this WebApplication app)
    {
        _ = app.MapGroup("/authentication").MapLoginAndLogout();
        return app;
    }
}
