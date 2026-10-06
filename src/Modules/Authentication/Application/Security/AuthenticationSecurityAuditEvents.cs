using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Audit.Domain;

namespace Vitarish.Authentication.Application.Security;

public static class AuthenticationSecurityAuditEvents
{
    public const string PasswordChange = SecurityAuditEventCatalog.AuthenticationPasswordChangeEvent;
    public const string PasswordLogin = SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent;
    public const string PasswordLoginOtp = SecurityAuditEventCatalog.AuthenticationPasswordLoginOtpEvent;
    public const string PhoneLogin = SecurityAuditEventCatalog.AuthenticationPhoneLoginEvent;
    public const string EmailLogin = SecurityAuditEventCatalog.AuthenticationEmailLoginEvent;
    public const string PasskeyLogin = SecurityAuditEventCatalog.AuthenticationPasskeyLoginEvent;
    public const string PasswordLoginOperation = SecurityAuditEventCatalog.PasswordLoginOperation;
    public const string PasswordLoginOtpOperation = SecurityAuditEventCatalog.PasswordLoginOtpOperation;
    public const string PhoneLoginOperation = SecurityAuditEventCatalog.PhoneLoginOperation;
    public const string EmailLoginOperation = SecurityAuditEventCatalog.EmailLoginOperation;
    public const string PasskeyLoginOperation = SecurityAuditEventCatalog.PasskeyLoginOperation;
    public const string AuthenticationAttemptIdempotencySuffix = ":authentication-attempt:";
    public const string OtpSmsSend = SecurityAuditEventCatalog.AuthenticationOtpSmsSendEvent;
    public const string OtpEmailSend = SecurityAuditEventCatalog.AuthenticationOtpEmailSendEvent;
    public const string OtpSmsSendOperation = SecurityAuditEventCatalog.AuthenticationOtpSmsSendOperation;
    public const string OtpEmailSendOperation = SecurityAuditEventCatalog.AuthenticationOtpEmailSendOperation;
    public const string OtpDeliveryResourcePrefix = "authentication/otp/";
    public const string OtpDeliveryIdempotencyPrefix = ":authentication-otp-delivery:";
    public const string OtpDeliveryFailure = SecurityAuditReasonCatalog.AuthenticationProviderFailure;
    public const string Resource = "authentication";
    public const string Operation = SecurityAuditEventCatalog.PasswordChangeOperation;
    public const string SourceBoundary = "authentication.application";
    public const string ResourcePrefix = "user/";
    public const string StartedKeySuffix = ":started";
    public const string CompletedKeySuffix = ":completed";
    public const string FailureAuditUnavailable = SecurityAuditReasonCatalog.AuditStoreUnavailable;
    public const string FailureMutationRejected = SecurityAuditReasonCatalog.MutationRejected;
    public const string FailurePasswordPolicyRejected = SecurityAuditReasonCatalog.PasswordPolicyRejected;
    public const string FailurePasswordLoginDisabled = SecurityAuditReasonCatalog.PasswordLoginDisabled;
    public const string FailureDuplicateInProgress = SecurityAuditReasonCatalog.DuplicateOutcomeUnconfirmed;
    public const string FailureDuplicatePayloadMismatch = SecurityAuditReasonCatalog.IdempotencyKeyReused;
    public const string SecurityAccessPolicy = SecurityAuditEventCatalog.AuthenticationSecurityAccessPolicyEvent;
    public const string SecurityAccessPolicyOperation = SecurityAuditEventCatalog.SecurityAccessPolicyOperation;
    public const string SecurityAccessPolicyResource = "authentication/security-access-policy";
    public const string SecurityAccessPolicyStartedSuffix = ":authentication-security-policy:started";
    public const string SecurityAccessPolicyCompletedSuffix = ":authentication-security-policy:completed";
    public const string SecurityAccessPolicyConcurrencyConflictReason = SecurityAuditReasonCatalog.SecurityPolicyConcurrencyConflict;
    public const string SecurityAccessPolicyValidationFailureReason = SecurityAuditReasonCatalog.SecurityPolicyValidationFailure;
    public const string SecurityAccessPolicyPersistenceFailureReason = SecurityAuditReasonCatalog.SecurityPolicyPersistenceFailure;
    public const string SecurityFailure = SecurityAuditEventCatalog.SecurityFailureEvent;
    public const string SecurityFailureOperation = SecurityAuditEventCatalog.SecurityFailureOperation;
    public const string SecurityFailureKeySuffix = ":security-failure";
    public const string SessionEstablished = SecurityAuditEventCatalog.SessionEstablishedEvent;
    public const string SessionEstablishedOperation = SecurityAuditEventCatalog.SessionEstablishedOperation;
    public const string SessionTerminated = SecurityAuditEventCatalog.SessionTerminatedEvent;
    public const string SessionTerminatedOperation = SecurityAuditEventCatalog.SessionTerminatedOperation;
    public const string SessionAccessHistory = SecurityAuditEventCatalog.SessionAccessHistoryEvent;
    public const string SessionAccessHistoryOperation = SecurityAuditEventCatalog.SessionAccessHistoryOperation;
    public const string SessionNotice = SecurityAuditEventCatalog.SessionNoticeEvent;
    public const string SessionNoticeOperation = SecurityAuditEventCatalog.SessionNoticeOperation;
    public const string SessionPolicy = SecurityAuditEventCatalog.SessionPolicyEvent;
    public const string SessionPolicyOperation = SecurityAuditEventCatalog.SessionPolicyOperation;
    public const string SessionConcurrentLimit = SecurityAuditReasonCatalog.ConcurrentSessionLimit;
    public const string SessionIpDenied = SecurityAuditReasonCatalog.SessionStartIpDenied;
    public const string SessionInactive = SecurityAuditReasonCatalog.InactivityTimeout;
    public const string SessionReplaced = SecurityAuditReasonCatalog.ReplacedByNewSession;
    public const string SessionAcknowledged = SecurityAuditReasonCatalog.Acknowledged;
    public const string SessionUserPolicyExpired = SecurityAuditReasonCatalog.UserPolicyExpired;
    public const string SessionAdministrativeRevocation = SecurityAuditReasonCatalog.AdministrativeRevocation;
    public const string SessionPolicyInvalidated = SecurityAuditReasonCatalog.PolicyInvalidated;
    public const string AwaitingMutationOutcome = SecurityAuditReasonCatalog.AwaitingMutationOutcome;
    public const string UserPolicyOperationSegment = ":user:";
    public const string UserSessionsOperationSegment = ":user-sessions:";
    public const string UserTimeoutMinutesReasonPrefix = SecurityAuditReasonCatalog.UserTimeoutMinutesReasonPrefix;
    public const string UserTimeoutDefaultReason = SecurityAuditReasonCatalog.UserTimeoutDefault;
    public const string UserPolicyTargetNotFoundReason = SecurityAuditReasonCatalog.UserNotFound;
    public const string DefaultEventKey = "event";
    public const string SecurityAttributesChanged = SecurityAuditEventCatalog.SecurityAttributesChangedEvent;
    public const string SecurityAttributesChangedOperation = SecurityAuditEventCatalog.SecurityAttributesChangedOperation;
    public const string AuthenticationFailed = SecurityAuditReasonCatalog.AuthenticationFailed;
    public const string ParallelSessionEstablished = AuthenticationSessionNotice.ParallelSessionEstablishedNoticeType;
    public const string Unauthorized = SecurityAuditReasonCatalog.Unauthorized;

    public static string UserTimeoutMinutesReason(int minutes) => SecurityAuditReasonCatalog.UserTimeoutMinutes(minutes);

    public static string SessionTerminationReason(AuthenticationSessionTerminationReason reason) => reason switch
    {
        AuthenticationSessionTerminationReason.UserLogout => SecurityAuditReasonCatalog.UserLogout,
        AuthenticationSessionTerminationReason.ReplacedByNewSession => SecurityAuditReasonCatalog.ReplacedByNewSession,
        AuthenticationSessionTerminationReason.InactivityTimeout => SecurityAuditReasonCatalog.InactivityTimeout,
        AuthenticationSessionTerminationReason.SecurityAttributeChanged => SecurityAuditReasonCatalog.SecurityAttributeChanged,
        AuthenticationSessionTerminationReason.CookieSignInFailed => SecurityAuditReasonCatalog.CookieSignInFailed,
        AuthenticationSessionTerminationReason.AdministrativeRevocation => SecurityAuditReasonCatalog.AdministrativeRevocation,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
    };
}
