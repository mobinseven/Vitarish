namespace Vitarish.Authentication.Application.Abstractions;

public interface IAuthenticationUtcClock
{
    DateTime UtcNow { get; }
}
