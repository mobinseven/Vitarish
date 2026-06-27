public static partial class IdentityDefinitions
{
    public const string UserIdClaim = Vitarish.Requests.Contracts.RequestContextClaimTypes.UserId;
    public const string ClientIdClaim = Vitarish.Requests.Contracts.RequestContextClaimTypes.ClientId;
    public const string TenantIdClaim = Vitarish.Requests.Contracts.RequestContextClaimTypes.TenantId;
    public const string OwnerIdClaim = Vitarish.Requests.Contracts.RequestContextClaimTypes.OwnerId;
    public const string PersonalScope = "personal";
    public const string PartyScope = "party";
    public const string RolesScope = "role";
    public const string PlatformAdmin = nameof(PlatformAdmin);
    public const string PlatformUser = nameof(PlatformUser);
    public const string TenantOwner = nameof(TenantOwner);
    public const string TenantUser = nameof(TenantUser);
    public const string AccessClaim = "access";
    public const string AccessScope = "access";
}
