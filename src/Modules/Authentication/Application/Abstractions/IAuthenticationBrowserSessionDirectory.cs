namespace Vitarish.Authentication.Application.Abstractions;

public sealed record AuthenticationBrowserSessionDescriptor(string SessionId, DateTime CreatedAt, DateTime LastActivityAt, bool IsCurrent);

public interface IAuthenticationBrowserSessionDirectory
{
    Task<IReadOnlyList<AuthenticationBrowserSessionDescriptor>> ListAsync(string userId, string sessionId, CancellationToken cancellationToken);
    Task<bool> OwnsAsync(string userId, string sessionId, CancellationToken cancellationToken);
}
