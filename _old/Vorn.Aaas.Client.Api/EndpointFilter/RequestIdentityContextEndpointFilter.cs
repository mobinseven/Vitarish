
using Microsoft.AspNetCore.Http;
using Vorn.Aaas.Client.Api.Internal;

namespace Vorn.Aaas.Client.Api.EndpointFilter;

public sealed class RequestIdentityContextEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        HttpContext http = context.HttpContext;

        using IDisposable? scope = RequestIdentityContextInitializer.Initialize<RequestIdentityContextEndpointFilter>(http);
        return await next(context);
    }
}
