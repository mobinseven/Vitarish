namespace Vitarish.Authentication.Application.Commands;

public sealed record ChangeSecuritySessionPolicyCommand(
    int MaximumConcurrentSessions,
    bool ConcurrentSessionsFunctionallyRequired,
    int DefaultInactivityTimeoutMinutes,
    IReadOnlyList<string>? AllowedSessionStartCidrs,
    long ExpectedVersion);
