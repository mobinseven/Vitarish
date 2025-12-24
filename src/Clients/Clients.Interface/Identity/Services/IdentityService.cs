using System.Security.Claims;

namespace Vitarish.Clients.Interface.Identity.Services;

internal class IdentityService : IIdentityService
{
    public ClaimsPrincipal? User { get; set; }
}
