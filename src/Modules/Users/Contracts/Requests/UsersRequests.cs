namespace Vitarish.Users.Contracts.Requests;

public sealed record CreatePatRequest(int? LifetimeDays = null, IEnumerable<string>? Scopes = null);
public sealed record WebAuthnCompleteRegisterRequest(string AttestationResponseJson);
public sealed record ListTenantMembersQueryParameters(string? Search = null, int? MaxResults = null);
public sealed record ProvisionTenantMemberRequest(
    string? UserId = null,
    string? Email = null,
    string? PhoneNumber = null,
    string? FirstName = null,
    string? LastName = null);
