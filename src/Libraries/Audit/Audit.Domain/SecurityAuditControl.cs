namespace Vitarish.Audit.Domain;

/// <summary>
/// Security-event control items represented by the authoritative audit catalog.
/// </summary>
public enum SecurityAuditControl
{
    SecurityFunctionLifecycle = 1,
    FailedSecurityRecordRead = 2,
    SecurityRecordRead = 3,
    SecurityConfigurationChange = 4,
    AuditStorageThresholdAction = 5,
    AuditStorageFailureAction = 6,
    UserDataIntegrityVerification = 7,
    AuthenticationMechanismUse = 8,
    AuthenticationOutcome = 9,
    PasswordVerification = 10,
    SecurityAttributeAssignment = 11,
    SecurityAttributeChange = 12,
    InactiveEntityOperation = 13,
    UserDataImport = 14,
    InformationExport = 15,
    SecurityBehaviorChange = 16,
    SecurityAdministration = 17,
    GroupRoleMembershipChange = 18,
    SecurityFunctionFailure = 19,
    SecurityCapabilityUnavailable = 20,
    SessionEstablishment = 21,
    ConcurrentSessionLimit = 22,
    IdleSessionTermination = 23,
    AdministratorIdleTermination = 24
}
