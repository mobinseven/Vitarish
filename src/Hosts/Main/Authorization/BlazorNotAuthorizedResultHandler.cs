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
        var isRazorComponentEndpoint =
            endpoint?.Metadata.GetMetadata<RootComponentMetadata>() is not null;

        return isRazorComponentEndpoint && (authorizeResult.Challenged || authorizeResult.Forbidden)
            ? next(context)
            : _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
