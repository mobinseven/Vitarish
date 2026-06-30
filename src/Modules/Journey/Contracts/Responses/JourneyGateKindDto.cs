namespace Vitarish.Journey.Contracts.Responses;

/// <summary>
/// DTO for journey gate kind.
/// </summary>
public enum JourneyGateKindDto
{
    ContactConfirmation = 1,
    ProfileCompletion = 2,
    RequiredConsent = 3,
    WorkspaceSelection = 4,
    AccountRecovery = 5,
    SuspiciousLogin = 6,
    PasskeyEnrollmentPrompt = 7,
    MfaEnrollmentPrompt = 8
}
