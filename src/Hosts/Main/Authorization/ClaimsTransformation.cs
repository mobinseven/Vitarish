using Duende.IdentityServer.Extensions;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Main.Authorization;

public class ClaimsTransformation(IHttpContextAccessor httpContextAccessor) : IClaimsTransformation
{
    private readonly HttpContext http = httpContextAccessor.HttpContext!;
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated == true)
        {
            ITenantsService tenantsService = http.RequestServices.GetRequiredService<ITenantsService>();
            IUsersService usersService = http.RequestServices.GetRequiredService<IUsersService>();
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
            {
                var roles = await usersService.GetUserRolesAsync(principal.GetSubjectId());

                if (roles != null)
                {
                    ClaimsIdentity identity = new(IdentityDefinitions.RolesScope);
                    foreach (var role in roles)
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
                    }
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
