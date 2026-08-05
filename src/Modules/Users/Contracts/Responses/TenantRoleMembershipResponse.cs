namespace Vitarish.Users.Contracts.Responses;

public sealed record TenantRoleMembershipResponse(Guid TenantId, string RoleSuffix, bool Assigned);
