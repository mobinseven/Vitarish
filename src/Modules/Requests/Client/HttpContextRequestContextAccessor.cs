using Microsoft.AspNetCore.Http;
using Vitarish.Requests.Contracts;

namespace Vitarish.Requests.Client;

public sealed class HttpContextRequestContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    IRequestContextResolver resolver) : IRequestContextAccessor
{
    public RequestContextResult GetCurrent()
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;
        return httpContext is null
            ? new RequestContextResult(null, new RequestContextFailure(RequestContextFailureKind.Unauthenticated, "No current HTTP context is available."))
            : resolver.Resolve(httpContext.User);
    }
}
