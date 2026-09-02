namespace Vitarish.Authentication.Contracts.Requests;

public sealed record ChangeSecuritySessionPolicyRequest(
    int MaximumConcurrentSessions,
    bool ConcurrentSessionsFunctionallyRequired,
    int DefaultInactivityTimeoutMinutes,
    IReadOnlyList<string>? AllowedSessionStartCidrs,
    long ExpectedVersion);
