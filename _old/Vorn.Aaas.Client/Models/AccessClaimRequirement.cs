using Microsoft.AspNetCore.Authorization;

namespace Vorn.Aaas.Client.Models;

interface IAccessAuthorizationRequirement : IAuthorizationRequirement { }
internal class AccessClaimRequirement : IAccessAuthorizationRequirement { }
internal class AdminAccessClaimRequirement : IAccessAuthorizationRequirement { }
