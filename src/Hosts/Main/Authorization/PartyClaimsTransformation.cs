using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Main.Authorization;

public class PartyClaimsTransformation(IHttpContextAccessor httpContextAccessor) : IClaimsTransformation
{
    private readonly HttpContext http = httpContextAccessor.HttpContext!;
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated == true)
        {
            ITenantsService? tenantsService = http.RequestServices.GetService<ITenantsService>();
            if (tenantsService is not null)
            {
                Tenant? tenant = await tenantsService.ResolveFromHostAsync(http, http.RequestAborted);
                if (tenant is not null)
                {
                    ClaimsIdentity identity = new(IdentityDefinitions.PartyScope);
                    string clientId = tenant.ClientId;
                    string ownerId = tenant.OwnerId.ToString();
                    string tenantId = tenant.Id.ToString();
                    identity.AddClaim(new Claim(IdentityDefinitions.ClientIdClaim, clientId));
                    identity.AddClaim(new Claim(IdentityDefinitions.OwnerIdClaim, ownerId));
                    identity.AddClaim(new Claim(IdentityDefinitions.TenantIdClaim, tenantId));

                    if (identity.Claims.Any())
                    {
                        principal.AddIdentity(identity);
                    }
                }
            }
        }

        return principal;
    }
}
