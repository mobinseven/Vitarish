using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Application.Handlers;

public sealed class PersonalBrowserSessionHandler(IAuthenticationBrowserSessionDirectory sessions,
    IAuthenticationSessionRegistry registry, IRequestContext request)
{
    public Task<IReadOnlyList<AuthenticationBrowserSessionDescriptor>> ListAsync(CancellationToken cancellationToken) =>
        request.UserId is { } userId && request.User?.FindFirst(AuthenticationSessionClaimTypes.SessionId)?.Value is { } sessionId
            ? sessions.ListAsync(userId, sessionId, cancellationToken)
            : Task.FromResult<IReadOnlyList<AuthenticationBrowserSessionDescriptor>>([]);

    public async Task<bool> RevokeAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (request.UserId is not { } userId || request.User?.FindFirst(AuthenticationSessionClaimTypes.SessionId) is null ||
            !await sessions.OwnsAsync(userId, sessionId, cancellationToken))
            return false;
        await registry.RevokeAsync(sessionId, AuthenticationSessionTerminationReason.UserLogout, cancellationToken);
        return true;
    }
}
