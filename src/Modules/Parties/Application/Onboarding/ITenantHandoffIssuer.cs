namespace Vitarish.Parties.Application.Onboarding;

/// <summary>Lets the creator continue, already signed in, on their new tenant's own host.</summary>
public interface ITenantHandoffIssuer
{
    /// <returns>A one-time token for <paramref name="hostname"/>, or null when no handoff can be issued; the creator then signs in normally.</returns>
    Task<string?> IssueAsync(string userId, string hostname, CancellationToken cancellationToken = default);
}
