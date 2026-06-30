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
                DestinationUrl: "/workspaces/select",
                Message: "Workspace or app selection is required."
            );
        }

        return null;
    }
}
