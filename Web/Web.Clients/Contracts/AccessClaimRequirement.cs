using Microsoft.AspNetCore.Authorization;

namespace Vitarish.Web.Clients.Contracts;

internal interface IAccessAuthorizationRequirement : IAuthorizationRequirement
{ }

internal class AccessClaimRequirement : IAccessAuthorizationRequirement { }

internal class AdminAccessClaimRequirement : IAccessAuthorizationRequirement { }
