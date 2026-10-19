using Microsoft.AspNetCore.Http;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Hosting;

internal sealed class ClientInterfaceHostAccessor(
    ClientOriginTrust originTrust,
    IHttpContextAccessor httpContextAccessor) : IClientInterfaceHostAccessor
{
    public string? ClientInterfaceHost
    {
        get
        {
            HttpContext? context = httpContextAccessor.HttpContext;
            if (context is null || !ClientOriginTrust.TryReadSingleOrigin(context.Request, out Uri origin, out string normalized))
                return null;

            return originTrust.IsExact(normalized) || ClientOriginTrust.IsTrustedForRequest(context, normalized)
                ? origin.Host
                : null;
        }
    }
}
