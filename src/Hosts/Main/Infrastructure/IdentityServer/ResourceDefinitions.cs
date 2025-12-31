using Duende.IdentityServer.Models;

public static partial class ResourceDefinitions
{
    public static IdentityResource[] IdentityResources { get; } = [
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResources.Phone(),
                new IdentityResource(IdentityDefinitions.PartyScope, "Party Information", [IdentityDefinitions.ClientIdClaim, IdentityDefinitions.OwnerIdClaim, IdentityDefinitions.TenantIdClaim])
            ];
}
