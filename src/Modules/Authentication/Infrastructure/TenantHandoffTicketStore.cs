using Microsoft.EntityFrameworkCore;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Sessions;

namespace Vitarish.Authentication.Infrastructure;

public sealed class TenantHandoffTicketStore(AuthenticationDbContext dbContext) : ITenantHandoffTicketStore
{
    public async Task AddAsync(TenantHandoffTicket ticket, DateTime nowUtc, CancellationToken cancellationToken)
    {
        List<TenantHandoffTicket> spent = await dbContext.TenantHandoffTickets
            .Where(item => item.UserId == ticket.UserId && (item.ExpiresAt <= nowUtc || item.RedeemedAt != null))
            .ToListAsync(cancellationToken);
        dbContext.TenantHandoffTickets.RemoveRange(spent);
        dbContext.TenantHandoffTickets.Add(ticket);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<TenantHandoffTicket?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.TenantHandoffTickets.SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);

    public async Task<bool> TryMarkRedeemedAsync(TenantHandoffTicket ticket, DateTime nowUtc, CancellationToken cancellationToken)
    {
        ticket.Redeem(nowUtc);
        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }
}
