using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application;

public static class RequestsContextExtensions
{
    extension(IRequestContext context)
    {
        public bool IsTenantAdmin() => context.User?.IsInRole($"{context.TenantId}:{IdentityAccessConstants.TenantAdmin}") ?? false;
        public bool IsTenantOwner() => context.User?.IsInRole($"{context.TenantId}:{IdentityAccessConstants.TenantOwner}") ?? false;
        public bool IsTenantUser() => context.User?.IsInRole($"{context.TenantId}:{IdentityAccessConstants.TenantUser}") ?? false;
        public string GetTenantUserRole() => $"{context.TenantId}:{IdentityAccessConstants.TenantUser}";
        public string GetTenantAdminRole() => $"{context.TenantId}:{IdentityAccessConstants.TenantAdmin}";
        public string GetTenantOwnerRole() => $"{context.TenantId}:{IdentityAccessConstants.TenantOwner}";
    }
}
