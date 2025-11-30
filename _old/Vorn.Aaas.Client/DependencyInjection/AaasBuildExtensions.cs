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
using Vorn.Aaas.Client.Abstractions;
using Vorn.Aaas.Client.Api.Options;
using Vorn.Aaas.Client.Constants;
using Vorn.Aaas.Client.Contracts;
using Vorn.Aaas.Client.Extensions;
using Vorn.Aaas.Client.Services;

public static class AaasBuildExtensions
{
    public const string DefaultChallengeScheme = "oidc";
    public static VornOptions GetVornOptions(this WebApplicationBuilder builder)
    {
        IConfigurationSection section = builder.Configuration.GetSection(VornOptions.Section);
        VornOptions vornOptions = section.Get<VornOptions>() ?? throw new InvalidOperationException($"Configuration section '{VornOptions.Section}' is missing or invalid.");
        return vornOptions;
    }
    public static async Task<WebApplicationBuilder> AddAaasClient(this WebApplicationBuilder builder)
    {
        if (builder.Services.Any(sd => sd.ServiceType == typeof(IAccessService)))
        {
            return builder;
        }
        VornOptions vornOptions = builder.GetVornOptions();

        _ = builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = DefaultChallengeScheme;
            options.DefaultSignOutScheme = DefaultChallengeScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, o =>
        {
            o.ExpireTimeSpan = TimeSpan.FromDays(vornOptions.Aaas.CookieExpirationDays);
            o.Cookie.MaxAge = TimeSpan.FromDays(vornOptions.Aaas.CookieExpirationDays);
            o.SlidingExpiration = true;
        })
        .AddOpenIdConnect(DefaultChallengeScheme, options =>
        {
            options.Authority = vornOptions.Aaas.Authority;
            options.ClientId = vornOptions.Aaas.ClientId;
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
                    context.ProtocolMessage.IdTokenHint = idToken;   // avoid “Are you sure?” page
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
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = vornOptions.Aaas.Authority;
                options.TokenValidationParameters.ValidateAudience = false;
                options.RequireHttpsMetadata = true;
                options.TokenValidationParameters.ValidTypes = ["at+jwt"];
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
        await builder.AddAaasDataProtection();
        _ = builder.Services.AddScoped<IAuthorizationHandler, AccessAuthorizationHandler>();
        _ = builder.Services.AddScoped<IUserAccessApiClient, UserAccessApiClient>();
        _ = builder.Services.AddScoped<IPersonalAccessTokenClient, PersonalAccessTokenClient>();
        _ = builder.Services.AddScoped<IAccessService, AccessService>();
        _ = builder.Services.AddScoped<UserProfileService>();
        _ = builder.Services.AddScoped<UserProfileCultureInfoProvider>();
        _ = builder.Services.AddAaasApiClient(builder.Configuration);

        return builder;
    }
}
