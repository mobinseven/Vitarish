using Vitarish.Authentication.Domain.Sessions;

namespace Vitarish.Authentication.Application.Abstractions;

public interface ITenantHandoffTicketStore
{
    /// <summary>Stores a new ticket and drops the user's tickets that can no longer be redeemed.</summary>
    Task AddAsync(TenantHandoffTicket ticket, DateTime nowUtc, CancellationToken cancellationToken);

    Task<TenantHandoffTicket?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Marks the ticket redeemed. False when a concurrent request redeemed it first, so exactly
    /// one redemption succeeds.
    /// </summary>
    Task<bool> TryMarkRedeemedAsync(TenantHandoffTicket ticket, DateTime nowUtc, CancellationToken cancellationToken);
}
