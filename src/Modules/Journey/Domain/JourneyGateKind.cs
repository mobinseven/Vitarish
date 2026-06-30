namespace Vitarish.Journey.Domain;

public enum JourneyGateKind
{
    ContactConfirmation,
    ProfileCompletion,
    RequiredConsent,
    WorkspaceSelection,
    AccountRecovery,
    SuspiciousLogin,
    PasskeyEnrollmentPrompt,
    MfaEnrollmentPrompt
}
