using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Vitarish.Clients.Interface.Identity.Extensions;

internal static class LoginLogoutEndpointRouteBuilderExtensions
{
    public const string DefaultChallengeScheme = "oidc";
    internal static IEndpointConventionBuilder MapLoginAndLogout(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("");
        string pathBase = "/";
        _ = group.MapGet("/login", (string? returnUrl) => TypedResults.Challenge(GetAuthProperties(returnUrl, pathBase)))
            .AllowAnonymous();
        _ = group.MapPost("/logout", async ([FromServices] IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
            [FromServices] IDistributedCache cache,
            [FromServices] IRequestContextClient identityContext,
            HttpContext context,
            [FromForm] string? returnUrl) =>
        {
            OpenIdConnectOptions oidcOptions = oidcOptionsMonitor.Get(DefaultChallengeScheme);

            string? userId = context.User.GetId();
            if (!string.IsNullOrEmpty(userId))
            {
                string? ownerOrClient = string.IsNullOrWhiteSpace(identityContext.Request?.OwnerId)
                    ? identityContext.Request?.ClientId
                    : identityContext.Request.OwnerId;
                ownerOrClient = string.IsNullOrWhiteSpace(ownerOrClient)
                    ? oidcOptions.ClientId
                    : ownerOrClient;

                if (!string.IsNullOrEmpty(ownerOrClient))
                {
                    string cacheKey = $"{userId}:{ownerOrClient}:{nameof(AccessClaimRequirement)}";
                    cache.Remove(cacheKey);
                    string cacheKeyAdmin = $"{userId}:{ownerOrClient}:{nameof(AdminAccessClaimRequirement)}";
                    cache.Remove(cacheKeyAdmin);
                }
            }

            await RevokeRefreshTokenAsync(context, oidcOptions);

            AuthenticationProperties props = GetAuthProperties(returnUrl, pathBase);
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme, props);
            await context.SignOutAsync(DefaultChallengeScheme, props);
        });

        return group;
    }

    private static AuthenticationProperties GetAuthProperties(string? returnUrl, string pathBase)
    {

        // Prevent open redirects.
        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = pathBase;
        }
        else if (!Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        {
            returnUrl = new Uri(returnUrl, UriKind.Absolute).PathAndQuery;
        }
        else if (returnUrl[0] != '/')
        {
            returnUrl = $"{pathBase}{returnUrl}";
        }

        return new AuthenticationProperties { RedirectUri = returnUrl };
    }

    private static async Task RevokeRefreshTokenAsync(HttpContext context, OpenIdConnectOptions oidcOptions)
    {
        string? refreshToken = await context.GetTokenAsync("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        if (oidcOptions.ConfigurationManager is null)
        {
            return;
        }

        Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration oidcConfiguration = await oidcOptions.ConfigurationManager.GetConfigurationAsync(context.RequestAborted);
        if (!oidcConfiguration.AdditionalData.TryGetValue("revocation_endpoint", out object? endpointObj))
        {
            return;
        }

        string? revocationEndpoint = endpointObj as string;
        if (string.IsNullOrEmpty(revocationEndpoint))
        {
            return;
        }

        TokenRevocationRequest request = new()
        {
            Address = revocationEndpoint,
            Token = refreshToken,
            TokenTypeHint = "refresh_token",
            ClientId = oidcOptions.ClientId ?? throw new InvalidDataException("oidcOptions.ClientId is not supplied"),
            ClientSecret = oidcOptions.ClientSecret,
        };

        TokenRevocationResponse response = await oidcOptions.Backchannel.RevokeTokenAsync(request, context.RequestAborted);
        _ = (response.HttpResponse?.EnsureSuccessStatusCode());
    }
}

