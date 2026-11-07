using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.ReturnUrls;
using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Domain;
using Vitarish.Modules.Base.Core.Identity;

namespace Vitarish.Authentication.Infrastructure.ReturnUrls;

/// <summary>
/// Resolves redirects by delegating to Journey for post-authentication routing.
/// Falls back to the legacy behavior when Journey is not available or user is not authenticated.
/// </summary>
public sealed class JourneyAwareRedirectResolver(
    IJourneyResolver journeyResolver,
    IHttpContextAccessor httpContextAccessor,
    ILogger<JourneyAwareRedirectResolver> logger,
    IAuthenticationCentralDestinationPolicy? centralDestinations = null
) : IAuthenticationRedirectResolver
{
    public string Resolve(string? requestedReturnUrl, bool requiresProfileCompletion)
    {
        // Fallback to legacy behavior for synchronous calls
        return requiresProfileCompletion ? "/account/profile/complete" : ReturnUrlPolicy.NormalizeOrDefault(requestedReturnUrl).Value;
    }

    public async Task<string> ResolveAsync(string? requestedReturnUrl, bool requiresProfileCompletion, CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext;
        string? userId = httpContext is not null && AuthenticatedUserIdResolver.Resolve(httpContext.User) is AuthenticatedUserIdResolution.Success resolvedUserId
            ? resolvedUserId.UserId.Value
            : null;
        return await ResolveAsync(requestedReturnUrl, requiresProfileCompletion, userId, cancellationToken);
    }

    public async Task<string> ResolveAsync(string? requestedReturnUrl, bool requiresProfileCompletion, string? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Fallback to legacy behavior when user id is not available
            return requiresProfileCompletion
                ? "/account/profile/complete"
                : ReturnUrlPolicy.NormalizeOrDefault(requestedReturnUrl).Value;
        }

        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            var request = new JourneyRequest(
                UserId: userId,
                RequestedReturnUrl: requestedReturnUrl,
                CurrentPath: httpContext?.Request.Path.Value,
                AppKey: null,
                WorkspaceId: null,
                IsAfterLogin: true
            );

            JourneyStep step = await journeyResolver.ResolveAsync(request, cancellationToken);
            if (step.BlockingGate is not null && !string.IsNullOrWhiteSpace(requestedReturnUrl) && ReturnUrlPolicy.IsSafeLocal(requestedReturnUrl))
            {
                int queryStart = step.DestinationUrl.IndexOf('?');
                if (queryStart < 0 || !QueryHelpers.ParseQuery(step.DestinationUrl[queryStart..]).ContainsKey(AuthenticationBrowserQuery.ReturnUrl))
                    return QueryHelpers.AddQueryString(step.DestinationUrl, AuthenticationBrowserQuery.ReturnUrl, requestedReturnUrl);
            }
            return step.DestinationUrl;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // App continuation must not bypass mandatory personal gates during a dependency failure.
            if (requestedReturnUrl is not null && centralDestinations?.Owns(requestedReturnUrl) == true)
                throw;
            logger.LogWarning(ex, "Journey resolution failed for user {UserId}, falling back to legacy redirect", userId);
            return requiresProfileCompletion
                ? "/account/profile/complete"
                : ReturnUrlPolicy.NormalizeOrDefault(requestedReturnUrl).Value;
        }
    }
}
