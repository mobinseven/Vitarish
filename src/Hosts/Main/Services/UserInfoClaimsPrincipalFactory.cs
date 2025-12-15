using Duende.IdentityModel;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Host.Services;

public class UserInfoClaimsPrincipalFactory(UserManager<User> userManager, IOptions<IdentityOptions> optionsAccessor)
: UserClaimsPrincipalFactory<User>(userManager, optionsAccessor), IUserClaimsPrincipalFactory<User>
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        ClaimsIdentity identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim(JwtClaimTypes.FamilyName, user.LastName ?? string.Empty));
        identity.AddClaim(new Claim(JwtClaimTypes.GivenName, user.FirstName ?? string.Empty));
        return identity;
    }
}
