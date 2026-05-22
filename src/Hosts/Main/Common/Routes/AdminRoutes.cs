namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    public static class PlatformAdmin
    {
        public const string Home = "/platformadmin";
        public const string Search = "/platformadmin/search";
        public const string Communications = "/platformadmin/communications";
        public const string Files = "/platformadmin/files";
        public const string Users = "/platformadmin/users";
        public const string UserTemplate = "/platformadmin/users/{userId:guid}";
        public const string Owners = "/platformadmin/owners";
        public const string OwnerTemplate = "/platformadmin/owners/{ownerId:guid}";
        public const string OwnerTenantTemplate = "/platformadmin/owners/{ownerId:guid}/tenants/{tenantId:guid}";

        // userId is a string (ASP.NET Identity), validated but not recast as Guid
        public static string User(string userId) => $"{Users}/{SlugValue(userId)}";
        public static string Owner(Guid ownerId) => $"{Owners}/{GuidValue(ownerId)}";
        public static string OwnerTenant(Guid ownerId, Guid tenantId) =>
            $"{Owners}/{GuidValue(ownerId)}/tenants/{GuidValue(tenantId)}";
    }
}
