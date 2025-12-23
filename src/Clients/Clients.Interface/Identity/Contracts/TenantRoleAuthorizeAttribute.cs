using Microsoft.AspNetCore.Authorization;
using Vitarish.Clients.Interface.Identity.Services;

namespace Vitarish.Clients.Interface.Identity.Contracts;

public sealed class TenantRoleAuthorizeAttribute : AuthorizeAttribute
{
    public TenantRoleAuthorizeAttribute(string role)
    {
        Policy = $"{TenantRolePolicyProvider.Prefix}{role}";
    }
}
