using System.Linq.Expressions;

namespace Vitarish.Audit.Domain;

public sealed record SecurityAuditCatalogEntry(
    string Category,
    string EventName,
    string Operation,
    bool Applicable = true,
    string? Note = null,
    IEnumerable<SecurityAuditControl>? Controls = null)
{
    public IReadOnlySet<SecurityAuditControl> ControlItems { get; } = (Controls ?? []).ToHashSet();

    public bool Matches(string eventName, string operation) =>
        string.Equals(EventName, eventName, StringComparison.Ordinal) &&
        (Operation == SecurityAuditEventCatalog.OperationWildcard || string.Equals(Operation, operation, StringComparison.Ordinal));
}

public static class SecurityAuditEventCatalog
{
    public const string OperationWildcard = "*";
    public const string EndpointFilterSourceBoundary = "main.endpoint-filter";
    public const string EndpointResourcePrefix = "endpoint/";
    public const string StartedKeySuffix = ":started";
    public const string CompletedKeySuffix = ":completed";
    public const string WriteFailureCode = "security_audit_write_unavailable";
    public const string TerminalIdempotencyPrefix = "security-audit-terminal:";
    public const string FunctionCategory = "function";
    public const string ReviewCategory = "audit-review";
    public const string ConfigurationCategory = "audit-configuration";
    public const string AuthenticationCategory = "authentication";
    public const string SecurityAttributeCategory = "security-attributes";
    public const string ImportExportCategory = "import-export";
    public const string ManagementCategory = "management";
    public const string SecurityFailureCategory = "security-failure";
    public const string SessionCategory = "session";

    public const string EndpointMutationEvent = "security.endpoint.mutation";
    public const string AuditReviewEvent = "security.audit.review";
    public const string AuditReviewOperation = "audit.security-events.list";
    public const string AuditReviewSourceBoundary = "audit.review";
    public const string AuditReviewIdempotencySuffix = ":audit-review";
    public const string AuditConfigurationEvent = "security.audit.configuration";
    public const string AuditConfigurationOperation = "audit.configuration.read";
    public const string AuditStorageThresholdEvent = "security.audit.storage-threshold";
    public const string AuditStorageThresholdOperation = "audit.storage.threshold";
    public const string AuthenticationPasswordChangeEvent = "authentication.account.password.change";
    public const string AuthenticationPasswordLoginEvent = "authentication.password.login";
    public const string AuthenticationPasswordLoginOtpEvent = "authentication.password.login.otp";
    public const string PasswordChangeOperation = "account.password.change";
    public const string PasswordLoginOperation = "password.login";
    public const string PasswordLoginOtpOperation = "password.login.otp";
    public const string AuthenticationSecurityAccessPolicyEvent = "authentication.security-access-policy";
    public const string SecurityAccessPolicyOperation = "Authentication_SecurityAccessPolicy_Change";
    public const string SecurityAttributesChangedEvent = "authentication.security-attributes.changed";
    public const string SecurityAttributesChangedOperation = "security-attributes.change";
    public const string ImportEvent = "security.data.import";
    public const string ExportEvent = "security.data.export";
    public const string IntegrityVerificationEvent = "security.data.integrity-verification";
    public const string ImportOperation = "data.import";
    public const string ExportOperation = "data.export";
    public const string IntegrityVerificationOperation = "data.integrity.verify";
    public const string ManagementEvent = "security.management.operation";
    public const string SecurityFailureEvent = "security.failure";
    public const string SessionEstablishedEvent = "authentication.session.established";
    public const string SessionEstablishedOperation = "session.establish";
    public const string SessionTerminatedEvent = "authentication.session.terminated";
    public const string SessionTerminatedOperation = "session.terminate";
    public const string SessionAccessHistoryEvent = "authentication.access-history";
    public const string SessionAccessHistoryOperation = "access-history";
    public const string SessionNoticeEvent = "authentication.session.notice";
    public const string SessionNoticeOperation = "session.notice";
    public const string SessionPolicyEvent = "authentication.session-policy";
    public const string SessionPolicyOperation = "session-policy.change";
    public const string TenantRoleReplacementEvent = "users.tenant-role-replacement";
    public const string TenantRoleReplacementOperation = "Users_ReplaceTenantRole";
    public const string GlobalRoleAssignmentEvent = "users.global-role-assignment";
    public const string GlobalRoleAssignmentOperation = "Users_AssignGlobalRole";
    public const string GlobalRoleRevocationEvent = "users.global-role-revocation";
    public const string GlobalRoleRevocationOperation = "Users_RevokeGlobalRole";
    public const string TenantMemberProvisioningEvent = "users.tenant-member-provisioning";
    public const string TenantMemberProvisioningOperation = "Users_ProvisionTenantMember";
    public const string TenantMemberRemovalEvent = "users.tenant-member-removal";
    public const string TenantMemberRemovalOperation = "Users_RemoveTenantMember";
    public const string AdministratorIdleTerminationEvent = "security.session.administrator-idle-termination";

    public static IReadOnlyList<SecurityAuditCatalogEntry> Entries { get; } =
    [
        new(FunctionCategory, EndpointMutationEvent, OperationWildcard, Controls: [SecurityAuditControl.SecurityFunctionLifecycle]),
        new(ReviewCategory, AuditReviewEvent, AuditReviewOperation, Controls: [SecurityAuditControl.FailedSecurityRecordRead, SecurityAuditControl.SecurityRecordRead, SecurityAuditControl.SecurityAdministration]),
        new(ConfigurationCategory, AuditConfigurationEvent, AuditConfigurationOperation, Controls: [SecurityAuditControl.SecurityConfigurationChange, SecurityAuditControl.SecurityBehaviorChange]),
        new(SecurityFailureCategory, AuditStorageThresholdEvent, AuditStorageThresholdOperation, Controls: [SecurityAuditControl.AuditStorageThresholdAction]),
        new(AuthenticationCategory, AuthenticationPasswordChangeEvent, PasswordChangeOperation, Controls: [SecurityAuditControl.AuthenticationMechanismUse, SecurityAuditControl.PasswordVerification, SecurityAuditControl.SecurityAttributeChange]),
        new(AuthenticationCategory, AuthenticationPasswordLoginEvent, PasswordLoginOperation, Controls: [SecurityAuditControl.AuthenticationMechanismUse, SecurityAuditControl.AuthenticationOutcome, SecurityAuditControl.PasswordVerification]),
        new(AuthenticationCategory, AuthenticationPasswordLoginOtpEvent, PasswordLoginOtpOperation, Controls: [SecurityAuditControl.AuthenticationMechanismUse, SecurityAuditControl.AuthenticationOutcome]),
        new(AuthenticationCategory, AuthenticationSecurityAccessPolicyEvent, SecurityAccessPolicyOperation, Controls: [SecurityAuditControl.SecurityConfigurationChange, SecurityAuditControl.SecurityBehaviorChange]),
        new(SecurityAttributeCategory, SecurityAttributesChangedEvent, SecurityAttributesChangedOperation, Controls: [SecurityAuditControl.SecurityAttributeAssignment, SecurityAuditControl.SecurityAttributeChange]),
        new(ImportExportCategory, ImportEvent, ImportOperation, Controls: [SecurityAuditControl.UserDataImport]),
        new(ImportExportCategory, ExportEvent, ExportOperation, Controls: [SecurityAuditControl.InformationExport]),
        new(ImportExportCategory, IntegrityVerificationEvent, IntegrityVerificationOperation, Controls: [SecurityAuditControl.UserDataIntegrityVerification]),
        new(ManagementCategory, ManagementEvent, "management.operation", Controls: [SecurityAuditControl.SecurityAdministration, SecurityAuditControl.InactiveEntityOperation, SecurityAuditControl.GroupRoleMembershipChange]),
        new(SecurityFailureCategory, SecurityFailureEvent, "security.failure", Controls: [SecurityAuditControl.SecurityFunctionFailure, SecurityAuditControl.SecurityCapabilityUnavailable, SecurityAuditControl.AuditStorageFailureAction]),
        new(SessionCategory, SessionEstablishedEvent, SessionEstablishedOperation, Controls: [SecurityAuditControl.SessionEstablishment, SecurityAuditControl.AuthenticationOutcome, SecurityAuditControl.ConcurrentSessionLimit]),
        new(SessionCategory, SessionTerminatedEvent, SessionTerminatedOperation, Controls: [SecurityAuditControl.IdleSessionTermination, SecurityAuditControl.SecurityAdministration]),
        new(SessionCategory, SessionAccessHistoryEvent, SessionAccessHistoryOperation, Controls: [SecurityAuditControl.SecurityRecordRead, SecurityAuditControl.FailedSecurityRecordRead]),
        new(SessionCategory, SessionNoticeEvent, SessionNoticeOperation, Controls: [SecurityAuditControl.SecurityRecordRead, SecurityAuditControl.SecurityAttributeChange]),
        new(SessionCategory, SessionPolicyEvent, SessionPolicyOperation, Controls: [SecurityAuditControl.SecurityConfigurationChange, SecurityAuditControl.SecurityBehaviorChange]),
        new(ManagementCategory, TenantRoleReplacementEvent, TenantRoleReplacementOperation, Controls: [SecurityAuditControl.GroupRoleMembershipChange, SecurityAuditControl.SecurityAdministration]),
        new(ManagementCategory, GlobalRoleAssignmentEvent, GlobalRoleAssignmentOperation, Controls: [SecurityAuditControl.GroupRoleMembershipChange, SecurityAuditControl.SecurityAdministration]),
        new(ManagementCategory, GlobalRoleRevocationEvent, GlobalRoleRevocationOperation, Controls: [SecurityAuditControl.GroupRoleMembershipChange, SecurityAuditControl.SecurityAdministration]),
        new(ManagementCategory, TenantMemberProvisioningEvent, TenantMemberProvisioningOperation, Controls: [SecurityAuditControl.SecurityAttributeAssignment, SecurityAuditControl.GroupRoleMembershipChange]),
        new(ManagementCategory, TenantMemberRemovalEvent, TenantMemberRemovalOperation, Controls: [SecurityAuditControl.SecurityAttributeAssignment, SecurityAuditControl.GroupRoleMembershipChange]),
        new(SessionCategory, AdministratorIdleTerminationEvent, "session.terminate.idle", false, "Optional administrator-initiated idle-session termination is not implemented.", [SecurityAuditControl.AdministratorIdleTermination])
    ];

    public static SecurityAuditCatalogEntry? Find(string eventName, string operation) =>
        Entries.FirstOrDefault(entry => entry.Applicable && entry.Matches(eventName, operation));

    public static bool IsCatalogued(SecurityAuditEvent auditEvent)
    {
        SecurityAuditCatalogEntry? entry = Find(auditEvent.EventName, auditEvent.Operation);
        return entry is not null && string.Equals(auditEvent.EventCategory, entry.Category, StringComparison.Ordinal);
    }

    public static Expression<Func<SecurityAuditEvent, bool>> CataloguedPredicate()
    {
        ParameterExpression item = Expression.Parameter(typeof(SecurityAuditEvent), "item");
        Expression? match = null;

        foreach (SecurityAuditCatalogEntry entry in Entries.Where(entry => entry.Applicable))
        {
            Expression current = Expression.AndAlso(
                Expression.Equal(Expression.Property(item, nameof(SecurityAuditEvent.EventName)), Expression.Constant(entry.EventName)),
                Expression.Equal(Expression.Property(item, nameof(SecurityAuditEvent.EventCategory)), Expression.Constant(entry.Category)));

            if (entry.Operation != OperationWildcard)
            {
                current = Expression.AndAlso(
                    current,
                    Expression.Equal(Expression.Property(item, nameof(SecurityAuditEvent.Operation)), Expression.Constant(entry.Operation)));
            }

            match = match is null ? current : Expression.OrElse(match, current);
        }

        return Expression.Lambda<Func<SecurityAuditEvent, bool>>(match ?? Expression.Constant(false), item);
    }
}
