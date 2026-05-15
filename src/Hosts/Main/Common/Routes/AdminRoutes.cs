namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class Admin
    {
        public const string Home = "/admin";
        public const string Search = "/admin/search";
        public const string Communications = "/admin/communications";
        public const string Users = "/admin/users";
        public const string UserTemplate = "/admin/users/{userId:guid}";
        public const string Owners = "/admin/owners";
        public const string OwnerTemplate = "/admin/owners/{ownerId:guid}";
        public const string OwnerTenantTemplate = "/admin/owners/{ownerId:guid}/tenants/{tenantId:guid}";

        // userId is a string (ASP.NET Identity), validated but not recast as Guid
        public static string User(string userId) => $"{Users}/{SlugValue(userId)}";
        public static string Owner(Guid ownerId) => $"{Owners}/{GuidValue(ownerId)}";
        public static string OwnerTenant(Guid ownerId, Guid tenantId) =>
            $"{Owners}/{GuidValue(ownerId)}/tenants/{GuidValue(tenantId)}";
    }
}
