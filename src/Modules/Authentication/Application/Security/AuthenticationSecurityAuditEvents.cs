namespace Vitarish.Authentication.Application.Security;

internal static class AuthenticationSecurityAuditEvents
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
}
