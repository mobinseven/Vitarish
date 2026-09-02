using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure;

// Composition tests and non-HTTP workers have no trusted host address. The
// Requests module replaces this fallback when the host boundary is present;
// session-start CIDR policy then fails closed when no address is available.
internal sealed class UnavailableClientIpAddressAccessor : IClientIpAddressAccessor
{
    public string? ClientIpAddress => null;
}
