using Microsoft.EntityFrameworkCore;
using Vitarish.Authentication.Application.Abstractions;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AuthenticationSessionRootReader(AuthenticationDbContext database) : IAuthenticationSessionRootReader
{
    public async Task<Guid?> FindAsync(string sessionId, string userId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(sessionId, out Guid id)) return null;
        var session = await database.AuthenticationSessions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        return session is null ? null : session.CentralSessionId ?? session.Id;
    }
}
