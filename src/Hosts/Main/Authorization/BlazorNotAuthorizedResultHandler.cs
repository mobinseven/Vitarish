namespace Vitarish.Main.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Components.Endpoints;

public sealed class BlazorNotAuthorizedResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        var endpoint = context.GetEndpoint();

        // Razor component endpoints have RootComponentMetadata/ComponentTypeMetadata
        var isRazorComponentEndpoint =
            endpoint?.Metadata.GetMetadata<RootComponentMetadata>() is not null;

        if (isRazorComponentEndpoint && (authorizeResult.Challenged || authorizeResult.Forbidden))
        {
            // Don't challenge/forbid (which would redirect with cookies).
            // Let Blazor render; <AuthorizeRouteView><NotAuthorized> can show UI.
            return next(context);
        }

        // Keep normal behavior for controllers/Razor Pages/APIs.
        return _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
