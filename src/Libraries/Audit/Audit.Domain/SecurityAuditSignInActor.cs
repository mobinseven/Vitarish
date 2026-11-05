namespace Vitarish.Audit.Domain;

/// <summary>
/// Resolves the actor of a sign-in attempt record. The attempt runs on an
/// anonymous request, so a succeeded attempt is attributed to the account it
/// authenticated; started, pending, and denied attempts keep the request's actor.
/// </summary>
public static class SecurityAuditSignInActor
{
    public static string? Resolve(string? requestUserId, string outcome, string? authenticatedSubjectId) =>
        requestUserId ?? (string.Equals(outcome, SecurityAuditOutcome.Succeeded, StringComparison.Ordinal) ? authenticatedSubjectId : null);
}
