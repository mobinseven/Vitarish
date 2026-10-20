namespace Vitarish.Journey.Domain;

/// <summary>
/// The kind of gate that blocks or prompts the user.
/// </summary>
public enum JourneyGateKind
{
    /// <summary>Contact confirmation is required (email or phone).</summary>
    ContactConfirmation = 1,

    /// <summary>Profile completion is required.</summary>
    ProfileCompletion = 2,

    /// <summary>Required legal consent is missing.</summary>
    RequiredConsent = 3,

    /// <summary>Workspace or app selection is required.</summary>
    WorkspaceSelection = 4,

    /// <summary>Account recovery is needed (delegated to Authentication).</summary>
    AccountRecovery = 5,

    /// <summary>Suspicious login or reauthentication required (delegated to Authentication).</summary>
    SuspiciousLogin = 6,

    /// <summary>Passkey enrollment prompt (non-blocking).</summary>
    PasskeyEnrollmentPrompt = 7,

    /// <summary>MFA enrollment prompt (non-blocking).</summary>
    MfaEnrollmentPrompt = 8,

    /// <summary>The user is on the sign-up host, whose only purpose is creating their own tenant.</summary>
    TenantCreation = 9
}
