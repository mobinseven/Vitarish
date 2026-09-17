using Vitarish.Audit.Domain;

namespace Vitarish.Users.Application.Security;

public static class UsersSecurityAuditEvents
{
    public const string TenantRoleReplacement = SecurityAuditEventCatalog.TenantRoleReplacementEvent;
    public const string TenantRoleReplacementOperation = SecurityAuditEventCatalog.TenantRoleReplacementOperation;
    public const string GlobalRoleAssignment = SecurityAuditEventCatalog.GlobalRoleAssignmentEvent;
    public const string GlobalRoleRevocation = SecurityAuditEventCatalog.GlobalRoleRevocationEvent;
    public const string GlobalRoleAssignmentOperation = SecurityAuditEventCatalog.GlobalRoleAssignmentOperation;
    public const string GlobalRoleRevocationOperation = SecurityAuditEventCatalog.GlobalRoleRevocationOperation;
    public const string SourceBoundary = "users.application";
    public const string UserResourcePrefix = "user/";
    public const string IdempotencySuffix = ":tenant-role-replacement";
    public const string GlobalScope = "global";
    public const string GlobalRoleAssignmentIdempotencySuffix = ":global-role-assignment";
    public const string GlobalRoleRevocationIdempotencySuffix = ":global-role-revocation";
    public const string TenantMemberProvisioning = SecurityAuditEventCatalog.TenantMemberProvisioningEvent;
    public const string TenantMemberProvisioningOperation = SecurityAuditEventCatalog.TenantMemberProvisioningOperation;
    public const string TenantMemberProvisioningIdempotencySuffix = ":tenant-member-provisioning";
    public const string TenantMemberRemoval = SecurityAuditEventCatalog.TenantMemberRemovalEvent;
    public const string TenantMemberRemovalOperation = SecurityAuditEventCatalog.TenantMemberRemovalOperation;
    public const string TenantMemberRemovalIdempotencySuffix = ":tenant-member-removal";
}
