namespace Vitarish.Requests.Application.Abstractions;

/// <summary>
/// Exposes the client address normalized by the trusted host boundary.
/// Authentication must not parse forwarded headers itself.
/// </summary>
public interface IClientIpAddressAccessor
{
    string? ClientIpAddress { get; }
}
