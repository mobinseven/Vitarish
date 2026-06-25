namespace Vitarish.Parties.Contracts.Routes;
public static class PartiesApiRoutes
{
    public const string Base = "/api/modules/parties";
    public const string Tenants = "/tenants";
    public const string TenantById = "/tenants/{tenantId:guid}";
    public const string TenantIndicators = "/tenant-indicators";
}
