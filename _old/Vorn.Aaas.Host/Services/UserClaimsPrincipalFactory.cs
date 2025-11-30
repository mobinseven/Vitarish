using Duende.IdentityModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Vorn.Aaas.Users.Domain.Entities;
public class AaasUserClaimsPrincipalFactory<TUser>(UserManager<TUser> userManager, IOptions<IdentityOptions> optionsAccessor) : UserClaimsPrincipalFactory<TUser>(userManager, optionsAccessor) where TUser : User
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(TUser user)
    {
        ClaimsIdentity identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(JwtClaimTypes.FamilyName, user.LastName ?? string.Empty));
        identity.AddClaim(new Claim(JwtClaimTypes.GivenName, user.FirstName ?? string.Empty));
        return identity;
    }
}
