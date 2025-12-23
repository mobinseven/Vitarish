using Microsoft.AspNetCore.Authorization;

namespace Vitarish.Clients.Interface.Identity.Contracts;

public sealed record TenantRoleRequirement(string Role) : IAuthorizationRequirement;
