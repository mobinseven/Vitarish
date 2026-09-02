namespace Vitarish.Authentication.Contracts.Responses;

public sealed record SecuritySessionPolicyDto(
    int MaximumConcurrentSessions,
    bool ConcurrentSessionsFunctionallyRequired,
    int DefaultInactivityTimeoutMinutes,
    IReadOnlyList<string> AllowedSessionStartCidrs,
    long Version);
