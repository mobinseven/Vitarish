public static partial class IdentityDefinitions
{
    public const string UserIdClaim = "sub";
    public const string ClientIdClaim = "client";
    public const string TenantIdClaim = "tenant";
    public const string OwnerIdClaim = "owner";
    public const string PersonalScope = "personal";
    public const string PartyScope = "party";
    public const string RolesScope = "role";
    public const string PlatformAdmin = nameof(PlatformAdmin);
    public const string PlatformUser = nameof(PlatformUser);
    public const string TenantAdmin = nameof(TenantAdmin);
    public const string TenantUser = nameof(TenantUser);
    public const string Tenant = nameof(Tenant);
    public const string TenantOwner = nameof(TenantOwner);
    public const string AccessClaim = "access";
    public const string AccessScope = "access";
}
