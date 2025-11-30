using Microsoft.AspNetCore.Authorization;

namespace Vorn.Aaas.Client.Contracts;

internal interface IAccessAuthorizationRequirement : IAuthorizationRequirement
{ }
internal class AccessClaimRequirement : IAccessAuthorizationRequirement { }
internal class AdminAccessClaimRequirement : IAccessAuthorizationRequirement { }
