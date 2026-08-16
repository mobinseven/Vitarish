namespace Vitarish.Authentication.Application.Security;

internal static class AuthenticationSecurityAuditEvents
{
    public const string PasswordChange = "authentication.account.password.change";
    public const string PasswordLogin = "authentication.password.login";
    public const string Operation = "account.password.change";
    public const string SourceBoundary = "authentication.application";
    public const string ResourcePrefix = "user/";
    public const string StartedKeySuffix = ":started";
    public const string CompletedKeySuffix = ":completed";
    public const string FailureAuditUnavailable = "audit_store_unavailable";
    public const string FailureMutationRejected = "mutation_rejected";
    public const string FailureDuplicateInProgress = "duplicate_outcome_unconfirmed";
}
