using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Contracts.Routes;

namespace Vitarish.Authentication.Api.Endpoints;

/// <summary>Interactive credentials are accepted only at the configured central host.</summary>
public sealed class CentralAuthenticationEndpointFilter(IAuthenticationHostDirectory hosts) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (hosts.SignInOrigin is not null && !await hosts.IsCentralHostAsync(context.HttpContext.RequestAborted))
            return Results.NotFound();
        return await next(context);
    }
}
