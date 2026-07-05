namespace Vitarish.Modules.Base.Core.Identity;

public sealed record TenantAccess(bool CanAccessTenantOwner, bool CanAccessTenantUser);
