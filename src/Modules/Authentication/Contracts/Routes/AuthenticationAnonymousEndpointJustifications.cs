namespace Vitarish.Authentication.Contracts.Routes;

public static class AuthenticationAnonymousEndpointJustifications
{
    public const string EntryGroup = "Authentication login, challenge, and antiforgery endpoints must be reachable before a user has an authenticated module session.";
    public const string PasswordRecovery = "authentication.password-recovery";
    public const string TenantHandoff = "authentication.tenant-handoff";
    public const string Continuation = "authentication.continuation";
    public const string PendingChallenge = "authentication.pending-challenge";
}
