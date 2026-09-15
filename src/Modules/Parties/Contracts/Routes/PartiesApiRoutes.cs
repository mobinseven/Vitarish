namespace Vitarish.Parties.Contracts.Routes;
public static class PartiesApiRoutes
{
    public const string DocumentTag = "Parties";
    public const string Base = "/api/modules/parties";
    public const string UserTenants = "/user-tenants";
    public const string Owners = "/owners";
    public const string OwnerById = "/owners/{ownerId:guid}";
    public const string Tenants = "/tenants";
    public const string TenantById = "/tenants/{tenantId:guid}";
    public const string TenantsByOwner = "/owners/{ownerId:guid}/tenants";
    public const string TenantIndicators = "/tenant-indicators";
    public const string TenantIndicatorsByTenant = "/tenants/{tenantId:guid}/indicators";
    public const string TenantIndicatorById = "/tenant-indicators/{tenantIndicatorId:guid}";
    public const string SetupTenant = "/tenant-setup";
    public const string Administration = "/administration";
    public const string AdministrationOwners = "/owners";
    public const string AdministrationOwnerById = "/owners/{ownerId:guid}";
    public const string AdministrationTenants = "/tenants";
    public const string AdministrationTenantById = "/tenants/{tenantId:guid}";
    public const string AdministrationTenantsByOwner = "/owners/{ownerId:guid}/tenants";
    public const string AdministrationTenantIndicatorsByTenant = "/tenants/{tenantId:guid}/indicators";
    public const string AdministrationTenantIndicatorById = "/tenant-indicators/{tenantIndicatorId:guid}";
    public const string Platform = "/platform";
    public const string PlatformTenants = "/tenants";
}
