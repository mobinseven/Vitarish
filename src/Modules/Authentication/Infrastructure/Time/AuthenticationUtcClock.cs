using Vitarish.Authentication.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure.Time;

internal sealed class AuthenticationUtcClock(TimeProvider timeProvider) : IAuthenticationUtcClock
{
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
}
