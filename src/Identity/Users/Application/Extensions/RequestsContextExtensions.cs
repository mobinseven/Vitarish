using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application;

public static class RequestsContextExtensions
{
    extension(IRequestContext context)
    {
        public bool IsTenantAdmin() => context.User?.IsInRole($"{context.TenantId}:{IdentityDefinitions.TenantAdmin}") ?? false;
        public bool IsTenantOwner() => context.User?.IsInRole($"{context.TenantId}:{IdentityDefinitions.TenantOwner}") ?? false;
        public bool IsTenantUser() => context.User?.IsInRole($"{context.TenantId}:{IdentityDefinitions.TenantUser}") ?? false;
        public string GetTenantUserRole() => $"{context.TenantId}:{IdentityDefinitions.TenantUser}";
        public string GetTenantAdminRole() => $"{context.TenantId}:{IdentityDefinitions.TenantAdmin}";
        public string GetTenantOwnerRole() => $"{context.TenantId}:{IdentityDefinitions.TenantOwner}";
    }
}
