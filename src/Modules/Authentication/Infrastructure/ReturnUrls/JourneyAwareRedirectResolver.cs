using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.ReturnUrls;
using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Domain;

namespace Vitarish.Authentication.Infrastructure.ReturnUrls;

/// <summary>
/// Resolves redirects by delegating to Journey for post-authentication routing.
/// Falls back to the legacy behavior when Journey is not available or user is not authenticated.
/// </summary>
public sealed class JourneyAwareRedirectResolver(
    IJourneyResolver journeyResolver,
    IHttpContextAccessor httpContextAccessor
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
        string? userId = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(userId))
        {
            // Fallback to legacy behavior when user is not authenticated
            return ReturnUrlPolicy.NormalizeOrDefault(requestedReturnUrl).Value;
        }

        try
        {
            var request = new JourneyRequest(
                UserId: userId,
                RequestedReturnUrl: requestedReturnUrl,
                CurrentPath: httpContext?.Request.Path.Value,
                AppKey: null,
                WorkspaceId: null,
                IsAfterLogin: true
            );

            JourneyStep step = await journeyResolver.ResolveAsync(request, cancellationToken);
            return step.DestinationUrl;
        }
        catch
        {
            // Fallback to legacy behavior if Journey resolution fails
            return ReturnUrlPolicy.NormalizeOrDefault(requestedReturnUrl).Value;
        }
    }
}
