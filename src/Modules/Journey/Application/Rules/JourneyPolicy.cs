using Vitarish.Journey.Domain;

namespace Vitarish.Journey.Application.Rules;

/// <summary>
/// Evaluates journey gates based on user state.
/// Profile completion is delegated to IProfileCompletionPolicy (Users/Profile).
/// </summary>
public static class JourneyPolicy
{
    /// <summary>
    /// Evaluates contact confirmation gates.
    /// </summary>
    public static JourneyGate? EvaluateContactConfirmation(JourneyProfileSnapshot profile)
    {
        if (!profile.HasConfirmedEmail && !profile.HasConfirmedPhone)
        {
            return new JourneyGate(
                JourneyGateKind.ContactConfirmation,
                IsBlocking: true,
                DestinationUrl: "/account/contact/confirm",
                Message: "At least one confirmed contact (email or phone) is required."
            );
        }

        return null;
    }

    /// <summary>The root page of the app the user signed in to.</summary>
    public const string AppRootUrl = "/";

    /// <summary>The page where a user chooses which workspace or clinic to continue to.</summary>
    public const string WorkspaceSelectionUrl = "/workspaces/select";

    /// <summary>
    /// Sends a user on the shared sign-in host, which serves nothing else, to choose which of their
    /// clinics to continue to.
    /// </summary>
    public static JourneyGate? EvaluateClinicSelection(bool isSignInHost) =>
        isSignInHost
            ? new JourneyGate(
                JourneyGateKind.WorkspaceSelection,
                IsBlocking: true,
                DestinationUrl: WorkspaceSelectionUrl,
                Message: "Choose a clinic to continue.")
            : null;

    /// <summary>The page where a user on the sign-up host creates their own tenant.</summary>
    public const string TenantCreationUrl = "/setup";

    /// <summary>
    /// Sends a user on the sign-up host, which serves nothing else, to create their own tenant.
    /// </summary>
    public static JourneyGate? EvaluateTenantCreation(bool isSignUpHost) =>
        isSignUpHost
            ? new JourneyGate(
                JourneyGateKind.TenantCreation,
                IsBlocking: true,
                DestinationUrl: TenantCreationUrl,
                Message: "Create your own tenant to continue.")
            : null;

    /// <summary>
    /// Evaluates consent gates.
    /// </summary>
    public static JourneyGate? EvaluateConsent(JourneyConsentSnapshot? consent)
    {
        if (consent is null)
            return null;

        if (!consent.HasAcceptedTerms)
        {
            return new JourneyGate(
                JourneyGateKind.RequiredConsent,
                IsBlocking: true,
                DestinationUrl: "/account/consents",
                Message: "Terms of service acceptance is required."
            );
        }

        if (!consent.HasAcceptedPrivacyPolicy)
        {
            return new JourneyGate(
                JourneyGateKind.RequiredConsent,
                IsBlocking: true,
                DestinationUrl: "/account/consents",
                Message: "Privacy policy acceptance is required."
            );
        }

        return null;
    }

    /// <summary>
    /// Evaluates workspace selection gates.
    /// </summary>
    public static JourneyGate? EvaluateWorkspaceSelection(JourneyWorkspaceSnapshot? workspace)
    {
        if (workspace is null || workspace.RequiresWorkspaceSelection)
        {
            return new JourneyGate(
                JourneyGateKind.WorkspaceSelection,
                IsBlocking: true,
                DestinationUrl: WorkspaceSelectionUrl,
                Message: "Workspace or app selection is required."
            );
        }

        return null;
    }
}
