using Vitarish.Requests.Application.Abstractions;

public static class RequestsContextExtensions
{
    extension(IRequestContext context)
    {
        public bool IsPlatformAdmin() => context.User?.IsInRole($"{IdentityDefinitions.PlatformAdmin}") ?? false;
        public bool IsTenantOwner() => context.User?.IsInRole($"{context.TenantId}:{IdentityDefinitions.TenantOwner}") ?? false;
        public bool IsTenantUser() => context.User?.IsInRole($"{context.TenantId}:{IdentityDefinitions.TenantUser}") ?? false;
        public string GetTenantUserRole() => $"{context.TenantId}:{IdentityDefinitions.TenantUser}";
        public string GetTenantOwnerRole() => $"{context.TenantId}:{IdentityDefinitions.TenantOwner}";
    }
}
