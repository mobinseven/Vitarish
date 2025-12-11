using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Vitarish.Clients.Identity.Options;
using Vitarish.Clients.Interface.Identity.Abstractions;
using Vitarish.Clients.Interface.Identity.Constants;
using Vitarish.Clients.Interface.Identity.Contracts;
using Vitarish.Clients.Interface.Identity.Extensions;
using Vitarish.Clients.Interface.Identity.Services;

public static class AaasBuildExtensions
{
    private const string ConfigSection = "Vitarish";
    public const string DefaultChallengeScheme = "oidc";

    public static IdentityClientOptions GetAaasOptions(this WebApplicationBuilder builder)
    {
        IConfigurationSection section = builder.Configuration.GetSection($"{ConfigSection}:Aaas");
        IdentityClientOptions aaasOptions = section.Get<IdentityClientOptions>() ?? throw new InvalidOperationException($"Configuration section '{ConfigSection}:Aaas' is missing or invalid.");
        return aaasOptions;
    }

    public static async Task<WebApplicationBuilder> AddAaasClient(this WebApplicationBuilder builder)
    {
        if (builder.Services.Any(sd => sd.ServiceType == typeof(IAccessService)))
        {
            return builder;
        }
        IdentityClientOptions aaasOptions = builder.GetAaasOptions();

        _ = builder.Services
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
        if (!builder.Services.Any(sd => sd.ServiceType == typeof(IDistributedCache)))
        {
            _ = builder.Services.AddDistributedMemoryCache();
        }
        _ = builder.Services.AddMemoryCache();
        _ = builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(policy: new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                                                                      .AddRequirements(new AccessClaimRequirement())
                                                                      .Build())
            .AddPolicy(AaasConstants.Admin, policy => policy.RequireAuthenticatedUser().AddRequirements(new AdminAccessClaimRequirement()).Build());
        _ = builder.Services.ConfigureCookieOidcRefresh(CookieAuthenticationDefaults.AuthenticationScheme, DefaultChallengeScheme);
        _ = builder.Services.AddCascadingAuthenticationState();
        _ = builder.Services.AddHttpContextAccessor();
        _ = builder.Services.AddHttpClient();
        _ = builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, BlazorAuthorizationMiddlewareResultHandler>();
        _ = builder.Services.AddIdentityClient(builder.Configuration);
        await builder.AddIdentityClientDataProtection();
        _ = builder.Services.AddScoped<IAuthorizationHandler, AccessAuthorizationHandler>();
        _ = builder.Services.AddScoped<IUserAccessApiClient, UserAccessApiClient>();
        _ = builder.Services.AddScoped<IPersonalAccessTokenClient, PersonalAccessTokenClient>();
        _ = builder.Services.AddScoped<IAccessService, AccessService>();
        _ = builder.Services.AddScoped<UserProfileService>();
        _ = builder.Services.AddScoped<UserProfileCultureInfoProvider>();

        return builder;
    }
}
