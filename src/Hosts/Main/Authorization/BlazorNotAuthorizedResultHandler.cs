
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Components.Endpoints;

namespace Vitarish.Main.Authorization;

public sealed class BlazorNotAuthorizedResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        Endpoint? endpoint = context.GetEndpoint();
        bool isRazorComponentEndpoint =
            endpoint?.Metadata.GetMetadata<RootComponentMetadata>() is not null;

        return isRazorComponentEndpoint && (authorizeResult.Challenged || authorizeResult.Forbidden)
            ? next(context)
            : _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
