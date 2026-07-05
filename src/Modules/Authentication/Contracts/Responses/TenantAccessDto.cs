namespace Vitarish.Authentication.Contracts.Responses;

public sealed record TenantAccessDto(bool CanAccessTenantOwner, bool CanAccessTenantUser);
