using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application;

public static class RequestsContextExtensions
{
    extension(IRequestContext context)
    {
        public bool IsTenantAdmin() => context.User?.IsInRole($"{context.TenantId}:{IdentityAccessConstants.Admin}") ?? false;
        public bool IsTenantOwner() => context.User?.IsInRole($"{context.TenantId}:{IdentityAccessConstants.Owner}") ?? false;
        public bool IsTenantUser() => context.User?.IsInRole($"{context.TenantId}:{IdentityAccessConstants.User}") ?? false;
    }
}
