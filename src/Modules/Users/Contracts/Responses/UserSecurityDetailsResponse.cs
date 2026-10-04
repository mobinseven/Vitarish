namespace Vitarish.Users.Contracts.Responses;

public sealed record UserSecurityDetailsResponse(
    UserSecurityIdentityResponse User,
    IReadOnlyList<string> GlobalRoles,
    IReadOnlyList<string> PermittedGlobalRoles,
    IReadOnlyList<TenantRoleMembershipResponse> TenantRoles,
    UserSecurityHistoryResponse? SecurityHistory,
    UserSecurityHistoryResponse? AuthenticationHistory,
    bool SecurityHistoryAvailable,
    bool AuthenticationHistoryAvailable);

public sealed record UserSecurityIdentityResponse(
    string Id,
    string? UserName,
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber,
    string? FullName,
    string? Revision);

public sealed record UserSecurityHistoryResponse(
    IReadOnlyList<UserSecurityEventResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record UserSecurityEventResponse(
    string EventName,
    string? EventCategory,
    string Outcome,
    string? ActorId,
    string Operation,
    DateTime OccurredAt,
    string? ClientInterfaceHost,
    bool IsAuthenticationEvent,
    string? RedactedReason);
