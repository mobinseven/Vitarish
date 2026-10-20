namespace Vitarish.Authentication.Domain.Sessions;

/// <summary>
/// A one-time ticket that lets a user who just created a tenant start a normal session on that
/// tenant's own host without signing in again. Only a hash of the token is kept; the ticket works
/// once, for <see cref="Lifetime"/>, for one user, and only on its target host.
/// </summary>
public sealed class TenantHandoffTicket
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);
    public const int TokenHashLength = 64;
    public const int TargetHostMaximumLength = 253;

    public Guid Id { get; set; }

    /// <summary>Lowercase hex SHA-256 of the raw token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    /// <summary>Lowercase hostname, without scheme or port, the ticket can be redeemed on.</summary>
    public string TargetHost { get; set; } = string.Empty;

    public DateTime IssuedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RedeemedAt { get; set; }

    public static TenantHandoffTicket Issue(string tokenHash, string userId, string targetHost, DateTime nowUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetHost);
        return new TenantHandoffTicket
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            UserId = userId,
            TargetHost = targetHost.Trim().TrimEnd('.').ToLowerInvariant(),
            IssuedAt = nowUtc,
            ExpiresAt = nowUtc + Lifetime
        };
    }

    public bool CanBeRedeemedOn(string host, DateTime nowUtc) =>
        RedeemedAt is null
        && nowUtc < ExpiresAt
        && string.Equals(TargetHost, host.Trim().TrimEnd('.'), StringComparison.OrdinalIgnoreCase);

    public void Redeem(DateTime nowUtc)
    {
        if (RedeemedAt is not null)
            throw new InvalidOperationException("The handoff ticket was already redeemed.");
        RedeemedAt = nowUtc;
    }
}
