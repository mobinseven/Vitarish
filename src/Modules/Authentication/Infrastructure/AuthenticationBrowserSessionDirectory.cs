using Microsoft.EntityFrameworkCore;
using Vitarish.Authentication.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AuthenticationBrowserSessionDirectory(AuthenticationDbContext database,
    IAuthenticationSecurityPolicyProvider policy, IAuthenticationSessionRegistry registry, TimeProvider clock) : IAuthenticationBrowserSessionDirectory
{
    public async Task<IReadOnlyList<AuthenticationBrowserSessionDescriptor>> ListAsync(string userId, string sessionId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out Guid id)) return [];
        var current = await database.AuthenticationSessions.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (current is null) return [];
        Guid rootId = current.CentralSessionId ?? current.Id;
        int timeout = await registry.GetUserInactivityTimeoutAsync(userId, cancellationToken)
            ?? (await policy.GetAsync(cancellationToken)).DefaultInactivityTimeoutMinutes;
        DateTime cutoff = clock.GetUtcNow().UtcDateTime.AddMinutes(-timeout);
        var roots = await database.AuthenticationSessions.AsNoTracking()
            .Where(item => item.UserId == userId && item.CentralSessionId == null && item.TerminatedAt == null && item.LastActivityAt > cutoff)
            .OrderByDescending(item => item.CreatedAt).ToListAsync(cancellationToken);
        return roots.Select(item => new AuthenticationBrowserSessionDescriptor(item.Id.ToString("N"), item.CreatedAt, item.LastActivityAt, item.Id == rootId)).ToArray();
    }

    public Task<bool> OwnsAsync(string userId, string sessionId, CancellationToken cancellationToken) =>
        Guid.TryParse(sessionId, out Guid id)
            ? database.AuthenticationSessions.AsNoTracking().AnyAsync(item => item.Id == id && item.UserId == userId && item.CentralSessionId == null, cancellationToken)
            : Task.FromResult(false);
}
