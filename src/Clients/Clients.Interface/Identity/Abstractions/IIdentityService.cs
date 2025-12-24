using System.Security.Claims;

namespace Vitarish.Clients.Interface.Identity.Abstractions;

public interface IIdentityService
{
    ClaimsPrincipal? User { get; set; }
}
