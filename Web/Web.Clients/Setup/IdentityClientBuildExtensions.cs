using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Vitarish.Web.Clients.Abstractions;
using Vitarish.Web.Clients.Constants;
using Vitarish.Web.Clients.Contracts;
using Vitarish.Web.Clients.Extensions;
using Vitarish.Web.Clients.Options;
using Vitarish.Web.Clients.Services;

namespace Vitarish.Web.Clients.Setup;

/// <summary>
/// Extension methods for adding identity client services.
/// </summary>
public static class IdentityClientBuildExtensions
{
    /// <summary>
    /// Default OIDC challenge scheme name.
    /// </summary>
    public const string DefaultChallengeScheme = "oidc";

    /// <summary>
    /// Gets the Vitarish options from configuration.
    /// </summary>
    public static VitarishOptions GetVitarishOptions(this WebApplicationBuilder builder)
    {
        IConfigurationSection section = builder.Configuration.GetSection(VitarishOptions.Section);
        VitarishOptions options = section.Get<VitarishOptions>()
            ?? throw new InvalidOperationException($"Configuration section '{VitarishOptions.Section}' is missing or invalid.");
        return options;
    }

    /// <summary>
    /// Adds identity client services to the application.
    /// </summary>
    public static WebApplicationBuilder AddIdentityClient(this WebApplicationBuilder builder)
    {
        if (builder.Services.Any(sd => sd.ServiceType == typeof(IAccessService)))
        {
            return builder;
        }

        VitarishOptions options = builder.GetVitarishOptions();

        _ = builder.Services
            .AddAuthentication(authOptions =>
            {
                authOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                authOptions.DefaultChallengeScheme = DefaultChallengeScheme;
                authOptions.DefaultSignOutScheme = DefaultChallengeScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, cookieOptions =>
            {
                cookieOptions.ExpireTimeSpan = TimeSpan.FromDays(options.Identity.CookieExpirationDays);
                cookieOptions.Cookie.MaxAge = TimeSpan.FromDays(options.Identity.CookieExpirationDays);
                cookieOptions.SlidingExpiration = true;
            })
            .AddOpenIdConnect(DefaultChallengeScheme, oidcOptions =>
            {
                oidcOptions.Authority = options.Identity.Authority;
                oidcOptions.ClientId = options.Identity.ClientId;
                oidcOptions.MapInboundClaims = false;
                oidcOptions.GetClaimsFromUserInfoEndpoint = true;
                oidcOptions.SaveTokens = true;
                oidcOptions.NonceCookie.Expiration = TimeSpan.FromMinutes(30);

                oidcOptions.Events.OnSignedOutCallbackRedirect += context =>
                {
                    context.Response.Redirect(context.Options.SignedOutRedirectUri);
                    context.HandleResponse();
                    return Task.CompletedTask;
                };

                oidcOptions.Events.OnRedirectToIdentityProviderForSignOut = context =>
                {
                    string? idToken = context.Properties.GetTokenValue("id_token");
                    if (!string.IsNullOrEmpty(idToken))
                    {
                        context.ProtocolMessage.IdTokenHint = idToken;
                    }
                    return Task.CompletedTask;
                };

                oidcOptions.Events.OnAuthenticationFailed = context =>
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

                oidcOptions.Events.OnRemoteFailure = context =>
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

                oidcOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(30)
                };
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwtOptions =>
            {
                jwtOptions.Authority = options.Identity.Authority;
                jwtOptions.TokenValidationParameters.ValidateAudience = false;
                jwtOptions.RequireHttpsMetadata = true;
                jwtOptions.TokenValidationParameters.ValidTypes = ["at+jwt"];
            });

        if (!builder.Services.Any(sd => sd.ServiceType == typeof(IDistributedCache)))
        {
            _ = builder.Services.AddDistributedMemoryCache();
        }

        _ = builder.Services.AddMemoryCache();

        _ = builder.Services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new AccessClaimRequirement())
                .Build())
            .AddPolicy(AuthConstants.Admin, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new AdminAccessClaimRequirement())
                .Build());

        _ = builder.Services.ConfigureCookieOidcRefresh(CookieAuthenticationDefaults.AuthenticationScheme, DefaultChallengeScheme);
        _ = builder.Services.AddCascadingAuthenticationState();
        _ = builder.Services.AddHttpContextAccessor();
        _ = builder.Services.AddHttpClient();

        _ = builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, BlazorAuthorizationMiddlewareResultHandler>();

        builder.AddIdentityDataProtection();

        // Configure options
        _ = builder.Services.Configure<VitarishOptions>(builder.Configuration.GetSection(VitarishOptions.Section));

        // Register services
        _ = builder.Services.AddScoped<RequestIdentityContext>();
        _ = builder.Services.AddScoped<IRequestIdentityContext>(sp => sp.GetRequiredService<RequestIdentityContext>());
        _ = builder.Services.AddTransient<IIdentityClientFactory, IdentityClientFactory>();
        _ = builder.Services.AddScoped<IAuthorizationHandler, AccessAuthorizationHandler>();
        _ = builder.Services.AddScoped<IUserAccessApiClient, UserAccessApiClient>();
        _ = builder.Services.AddScoped<IPersonalAccessTokenClient, PersonalAccessTokenClient>();
        _ = builder.Services.AddScoped<IAccessService, AccessService>();
        _ = builder.Services.AddScoped<UserProfileService>();
        _ = builder.Services.AddScoped<IUserProfileService>(sp => sp.GetRequiredService<UserProfileService>());
        _ = builder.Services.AddScoped<UserProfileCultureInfoProvider>();
        _ = builder.Services.AddScoped<IUserProfileCultureInfoProvider>(sp => sp.GetRequiredService<UserProfileCultureInfoProvider>());

        return builder;
    }
}
