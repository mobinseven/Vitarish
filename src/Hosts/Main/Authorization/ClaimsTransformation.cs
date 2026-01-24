using Duende.IdentityModel;
using Duende.IdentityServer.Extensions;
using Vitarish.Parties.Domain.Entities;

namespace Vitarish.Main.Authorization;

public class ClaimsTransformation(IHttpContextAccessor httpContextAccessor) : IClaimsTransformation
{
    private readonly HttpContext http = httpContextAccessor.HttpContext!;
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated == true)
        {
            {
                ITenantResolver tenantsService = http.RequestServices.GetRequiredService<ITenantResolver>();
                Tenant? tenant = await tenantsService.ResolveAsync(http, http.RequestAborted);
                if (tenant is not null)
                {
                    ClaimsIdentity partyIdentity = new(IdentityDefinitions.PartyScope);
                    string clientId = tenant.ClientId;
                    string ownerId = tenant.OwnerId.ToString();
                    string tenantId = tenant.Id.ToString();
                    partyIdentity.AddClaim(new Claim(IdentityDefinitions.ClientIdClaim, clientId));
                    partyIdentity.AddClaim(new Claim(IdentityDefinitions.OwnerIdClaim, ownerId));
                    partyIdentity.AddClaim(new Claim(IdentityDefinitions.TenantIdClaim, tenantId));

                    if (partyIdentity.Claims.Any())
                    {
                        principal.AddIdentity(partyIdentity);
                    }
                }
            }
            {
                IUsersService usersService = http.RequestServices.GetRequiredService<IUsersService>();

                string userId = principal.GetSubjectId();

                User? user = await usersService.GetUserAsync(userId);

                ClaimsIdentity personalIdentity = new(IdentityDefinitions.PersonalScope);

                personalIdentity.AddClaim(new Claim(JwtClaimTypes.FamilyName, user.LastName ?? string.Empty));
                personalIdentity.AddClaim(new Claim(JwtClaimTypes.GivenName, user.FirstName ?? string.Empty));
                principal.AddIdentity(personalIdentity);

                IEnumerable<IdentityRole> roles = await usersService.GetUserRolesAsync(userId);

                if (roles != null)
                {
                    ClaimsIdentity rolesIdentity = new(IdentityDefinitions.RolesScope);
                    foreach (IdentityRole role in roles)
                    {
                        rolesIdentity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
                    }
                    if (rolesIdentity.Claims.Any())
                    {
                        principal.AddIdentity(rolesIdentity);
                    }
                }
            }
        }

        return principal;
    }
}
