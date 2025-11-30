
using Microsoft.AspNetCore.Http;
using Vorn.Aaas.Client.Api.Internal;

namespace Vorn.Aaas.Client.Api.Filters;

public sealed class RequestIdentityContextEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        HttpContext http = context.HttpContext;

        using IDisposable? scope = await RequestIdentityContextInitializer.Initialize<RequestIdentityContextEndpointFilter>(http);
        return await next(context);
    }
}
