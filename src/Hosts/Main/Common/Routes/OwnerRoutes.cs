namespace Vitarish.Main.Common;

public static partial class AppRoutes
{
    /// <summary>
    /// Route constants for the platform owner experience.
    /// All paths are under /owner/ and follow lowercase kebab-case.
    /// </summary>
    public static class Owners
    {
        public const string Dashboard = "/owner/dashboard";
        public const string Tenants = "/owner/tenants";
        public const string CreateTenant = "/owner/tenants/create";
        public const string TenantTemplate = "/owner/tenants/{tenantId:guid}";
        public const string TenantSettingsTemplate = "/owner/tenants/{tenantId:guid}/settings";
        public const string TenantMembersTemplate = "/owner/tenants/{tenantId:guid}/members";
        public const string TenantInviteTemplate = "/owner/tenants/{tenantId:guid}/members/invite";
        public const string TenantBillingTemplate = "/owner/tenants/{tenantId:guid}/billing";
        public const string TenantAuditTemplate = "/owner/tenants/{tenantId:guid}/audit";
        public const string TenantDangerTemplate = "/owner/tenants/{tenantId:guid}/danger";

        public static string Tenant(Guid tenantId) => $"/owner/tenants/{GuidValue(tenantId)}";
        public static string TenantSettings(Guid tenantId) => $"/owner/tenants/{GuidValue(tenantId)}/settings";
        public static string TenantMembers(Guid tenantId) => $"/owner/tenants/{GuidValue(tenantId)}/members";
        public static string TenantInvite(Guid tenantId) => $"/owner/tenants/{GuidValue(tenantId)}/members/invite";
        public static string TenantBilling(Guid tenantId) => $"/owner/tenants/{GuidValue(tenantId)}/billing";
        public static string TenantAudit(Guid tenantId) => $"/owner/tenants/{GuidValue(tenantId)}/audit";
        public static string TenantDanger(Guid tenantId) => $"/owner/tenants/{GuidValue(tenantId)}/danger";
    }
}
