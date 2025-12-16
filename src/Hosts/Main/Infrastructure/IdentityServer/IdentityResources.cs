using Duende.IdentityServer.Models;

namespace Vitarish.Main.Infrastructure.IdentityServer;

public static partial class IdentityDefinitions
{
    public const string TenantIdClaim = "tenant_id";
    public const string OwnerIdClaim = "owner_id";
    public const string TenantOwnerScope = "tenant_owner";

    public static IdentityResource[] IdentityResources = [
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResources.Phone(),
                new IdentityResource(TenantOwnerScope, "Tenant and Owner Information", [TenantIdClaim, OwnerIdClaim])
            ];
}
