using Vitarish.Authentication.Domain.Sessions;

namespace Vitarish.Authentication.Application.Security;

public static class AuthenticationSecurityAuditEvents
{
    public const string PasswordChange = "authentication.account.password.change";
    public const string PasswordLogin = "authentication.password.login";
    public const string PasswordLoginOtp = "authentication.password.login.otp";
    public const string PasswordLoginOperation = "password.login";
    public const string PasswordLoginOtpOperation = "password.login.otp";
    public const string Resource = "authentication";
    public const string Operation = "account.password.change";
    public const string SourceBoundary = "authentication.application";
    public const string ResourcePrefix = "user/";
    public const string StartedKeySuffix = ":started";
    public const string CompletedKeySuffix = ":completed";
    public const string FailureAuditUnavailable = "audit_store_unavailable";
    public const string FailureMutationRejected = "mutation_rejected";
    public const string FailurePasswordPolicyRejected = "password_policy_rejected";
    public const string FailurePasswordLoginDisabled = "password_login_disabled";
    public const string FailureDuplicateInProgress = "duplicate_outcome_unconfirmed";
    public const string FailureDuplicatePayloadMismatch = "idempotency_key_reused";
    public const string SecurityAccessPolicy = "authentication.security-access-policy";
    public const string SecurityAccessPolicyOperation = "Authentication_SecurityAccessPolicy_Change";
    public const string SecurityAccessPolicyResource = "authentication/security-access-policy";
    public const string SecurityAccessPolicyStartedSuffix = ":authentication-security-policy:started";
    public const string SecurityAccessPolicyCompletedSuffix = ":authentication-security-policy:completed";
    public const string SecurityAccessPolicyConcurrencyConflictReason = "concurrency_conflict";
    public const string SecurityAccessPolicyValidationFailureReason = "validation_failed";
    public const string SecurityAccessPolicyPersistenceFailureReason = "persistence_failure";
    public const string SessionEstablished = "authentication.session.established";
    public const string SessionEstablishedOperation = "session.establish";
    public const string SessionTerminated = "authentication.session.terminated";
    public const string SessionTerminatedOperation = "session.terminate";
    public const string SessionAccessHistory = "authentication.access-history";
    public const string SessionAccessHistoryOperation = "access-history";
    public const string SessionNotice = "authentication.session.notice";
    public const string SessionNoticeOperation = "session.notice";
    public const string SessionPolicy = "authentication.session-policy";
    public const string SessionPolicyOperation = "session-policy.change";
    public const string SessionConcurrentLimit = "concurrent_session_limit";
    public const string SessionIpDenied = "session_start_ip_denied";
    public const string SessionInactive = "inactivity_timeout";
    public const string SessionReplaced = "replaced_by_new_session";
    public const string SessionAcknowledged = "acknowledged";
    public const string SessionUserPolicyExpired = "user_policy_expired";
    public const string SessionPolicyInvalidated = "policy_invalidated";
    public const string AwaitingMutationOutcome = "awaiting_mutation_outcome";
    public const string UserPolicyOperationSegment = ":user:";
    public const string UserTimeoutMinutesReasonPrefix = "user_timeout_minutes:";
    public const string UserTimeoutDefaultReason = "user_timeout_default";
    public const string DefaultEventKey = "event";
    public const string SecurityAttributesChanged = "authentication.security-attributes.changed";
    public const string SecurityAttributesChangedOperation = "security-attributes.change";
    public const string AuthenticationFailed = "authentication_failed";
    public const string ParallelSessionEstablished = AuthenticationSessionNotice.ParallelSessionEstablishedNoticeType;
    public const string Unauthorized = "unauthorized";
}
