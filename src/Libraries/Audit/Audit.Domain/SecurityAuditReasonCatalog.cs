using System.Globalization;

namespace Vitarish.Audit.Domain;

public static class SecurityAuditReasonCatalog
{
    public const string EndpointException = "endpoint_exception";
    public const string AuditStoreUnavailable = "audit_store_unavailable";
    public const string MutationRejected = "mutation_rejected";
    public const string PasswordPolicyRejected = "password_policy_rejected";
    public const string PasswordLoginDisabled = "password_login_disabled";
    public const string DuplicateOutcomeUnconfirmed = "duplicate_outcome_unconfirmed";
    public const string IdempotencyKeyReused = "idempotency_key_reused";
    public const string SecurityPolicyConcurrencyConflict = "concurrency_conflict";
    public const string SecurityPolicyValidationFailure = "validation_failed";
    public const string SecurityPolicyPersistenceFailure = "persistence_failure";
    public const string ConcurrentSessionLimit = "concurrent_session_limit";
    public const string SessionStartIpDenied = "session_start_ip_denied";
    public const string InactivityTimeout = "inactivity_timeout";
    public const string ReplacedByNewSession = "replaced_by_new_session";
    public const string Acknowledged = "acknowledged";
    public const string UserPolicyExpired = "user_policy_expired";
    public const string PolicyInvalidated = "policy_invalidated";
    public const string AwaitingMutationOutcome = "awaiting_mutation_outcome";
    public const string UserTimeoutDefault = "user_timeout_default";
    public const string AuthenticationFailed = "authentication_failed";
    public const string IntegrityValidationFailed = "integrity_validation_failed";
    public const string Unauthorized = "unauthorized";
    public const string UserLogout = "user_logout";
    public const string SecurityAttributeChanged = "security_attribute_changed";
    public const string CookieSignInFailed = "cookie_sign_in_failed";
    public const string AdministrativeRevocation = "administrative_revocation";
    public const string UserTimeoutMinutesReasonPrefix = "user_timeout_minutes:";

    private const string HttpStatusPrefix = "http_";
    private const string VersionPrefix = "version:";

    private static readonly HashSet<string> KnownReasons = new(StringComparer.Ordinal)
    {
        EndpointException,
        AuditStoreUnavailable,
        MutationRejected,
        PasswordPolicyRejected,
        PasswordLoginDisabled,
        DuplicateOutcomeUnconfirmed,
        IdempotencyKeyReused,
        SecurityPolicyConcurrencyConflict,
        SecurityPolicyValidationFailure,
        SecurityPolicyPersistenceFailure,
        ConcurrentSessionLimit,
        SessionStartIpDenied,
        InactivityTimeout,
        ReplacedByNewSession,
        Acknowledged,
        UserPolicyExpired,
        PolicyInvalidated,
        AwaitingMutationOutcome,
        UserTimeoutDefault,
        AuthenticationFailed,
        IntegrityValidationFailed,
        Unauthorized,
        UserLogout,
        SecurityAttributeChanged,
        CookieSignInFailed,
        AdministrativeRevocation
    };

    public static string HttpStatus(int statusCode)
    {
        if (statusCode is < 400 or > 599)
            throw new ArgumentOutOfRangeException(nameof(statusCode));

        return $"{HttpStatusPrefix}{statusCode}";
    }

    public static string Version(long version)
    {
        if (version < 0)
            throw new ArgumentOutOfRangeException(nameof(version));

        return $"{VersionPrefix}{version}";
    }

    public static string UserTimeoutMinutes(int minutes)
    {
        if (minutes <= 0)
            throw new ArgumentOutOfRangeException(nameof(minutes));

        return $"{UserTimeoutMinutesReasonPrefix}{minutes}";
    }

    public static bool IsAllowed(string reason)
    {
        if (KnownReasons.Contains(reason))
            return true;

        return IsHttpStatus(reason) || IsVersion(reason) || IsUserTimeoutMinutes(reason);
    }

    private static bool IsHttpStatus(string reason)
    {
        string suffix = reason.StartsWith(HttpStatusPrefix, StringComparison.Ordinal)
            ? reason[HttpStatusPrefix.Length..]
            : string.Empty;

        return suffix.Length == 3 && IsAsciiDigits(suffix) &&
               int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out int statusCode) &&
               statusCode is >= 400 and <= 599;
    }

    private static bool IsVersion(string reason) =>
        IsNonNegativeInteger(reason, VersionPrefix);

    private static bool IsUserTimeoutMinutes(string reason)
    {
        string suffix = reason.StartsWith(UserTimeoutMinutesReasonPrefix, StringComparison.Ordinal)
            ? reason[UserTimeoutMinutesReasonPrefix.Length..]
            : string.Empty;

        return suffix.Length > 0 && IsAsciiDigits(suffix) &&
               int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out int minutes) &&
               minutes > 0;
    }

    private static bool IsNonNegativeInteger(string reason, string prefix)
    {
        string suffix = reason.StartsWith(prefix, StringComparison.Ordinal)
            ? reason[prefix.Length..]
            : string.Empty;

        return suffix.Length > 0 && IsAsciiDigits(suffix) &&
               long.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out long version) &&
               version >= 0;
    }

    private static bool IsAsciiDigits(string value)
    {
        foreach (char character in value)
        {
            if (character is < '0' or > '9')
                return false;
        }

        return true;
    }
}
