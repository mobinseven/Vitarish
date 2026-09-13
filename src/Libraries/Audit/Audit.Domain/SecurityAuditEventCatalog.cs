namespace Vitarish.Audit.Domain;

public sealed record SecurityAuditCatalogEntry(
    string Category,
    string EventName,
    string Operation,
    bool Applicable = true,
    string? Note = null)
{
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
    public const string AdministratorIdleTerminationEvent = "security.session.administrator-idle-termination";

    public static IReadOnlyList<SecurityAuditCatalogEntry> Entries { get; } =
    [
        new(FunctionCategory, EndpointMutationEvent, OperationWildcard),
        new(ReviewCategory, AuditReviewEvent, AuditReviewOperation),
        new(ConfigurationCategory, AuditConfigurationEvent, AuditConfigurationOperation),
        new(AuthenticationCategory, AuthenticationPasswordChangeEvent, PasswordChangeOperation),
        new(AuthenticationCategory, AuthenticationPasswordLoginEvent, PasswordLoginOperation),
        new(AuthenticationCategory, AuthenticationPasswordLoginOtpEvent, PasswordLoginOtpOperation),
        new(AuthenticationCategory, AuthenticationSecurityAccessPolicyEvent, SecurityAccessPolicyOperation),
        new(SecurityAttributeCategory, SecurityAttributesChangedEvent, SecurityAttributesChangedOperation),
        new(ImportExportCategory, ImportEvent, ImportOperation),
        new(ImportExportCategory, ExportEvent, ExportOperation),
        new(ImportExportCategory, IntegrityVerificationEvent, IntegrityVerificationOperation),
        new(ManagementCategory, ManagementEvent, "management.operation"),
        new(SecurityFailureCategory, SecurityFailureEvent, "security.failure"),
        new(SessionCategory, SessionEstablishedEvent, SessionEstablishedOperation),
        new(SessionCategory, SessionTerminatedEvent, SessionTerminatedOperation),
        new(SessionCategory, SessionAccessHistoryEvent, SessionAccessHistoryOperation),
        new(SessionCategory, SessionNoticeEvent, SessionNoticeOperation),
        new(SessionCategory, SessionPolicyEvent, SessionPolicyOperation),
        new(ManagementCategory, TenantRoleReplacementEvent, TenantRoleReplacementOperation),
        new(ManagementCategory, GlobalRoleAssignmentEvent, GlobalRoleAssignmentOperation),
        new(ManagementCategory, GlobalRoleRevocationEvent, GlobalRoleRevocationOperation),
        new(SessionCategory, AdministratorIdleTerminationEvent, "session.terminate.idle", false, "Optional administrator-initiated idle-session termination is not implemented.")
    ];

    public static SecurityAuditCatalogEntry? Find(string eventName, string operation) =>
        Entries.FirstOrDefault(entry => entry.Applicable && entry.Matches(eventName, operation));
}
