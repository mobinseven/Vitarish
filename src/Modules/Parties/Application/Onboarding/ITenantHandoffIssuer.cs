namespace Vitarish.Parties.Application.Onboarding;

/// <summary>Lets a signed-in user continue, already signed in, on another tenant host.</summary>
public interface ITenantHandoffIssuer
{
    /// <returns>A one-time token for <paramref name="hostname"/>, or null when no handoff can be issued; the creator then signs in normally.</returns>
    Task<string?> IssueAsync(string userId, string hostname, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a token like <see cref="IssueAsync"/> and signs the current session out, so the
    /// issuing host keeps no session of its own.
    /// </summary>
    /// <returns>The token, or null when no handoff was issued or the session could not be ended.</returns>
    Task<string?> IssueEndingSessionAsync(string userId, string hostname, CancellationToken cancellationToken = default);
}
