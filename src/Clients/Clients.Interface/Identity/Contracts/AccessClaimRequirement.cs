using Microsoft.AspNetCore.Authorization;

namespace Vitarish.Clients.Interface.Identity.Contracts;

internal interface IAccessAuthorizationRequirement : IAuthorizationRequirement
{ }
internal class AccessClaimRequirement : IAccessAuthorizationRequirement { }
internal class AdminAccessClaimRequirement : IAccessAuthorizationRequirement { }
