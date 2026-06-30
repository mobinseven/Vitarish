namespace Vitarish.Journey.Domain;

/// <summary>
/// Snapshot of the user's consent state for journey evaluation.
/// </summary>
public sealed record JourneyConsentSnapshot(
    bool HasAcceptedTerms,
    bool HasAcceptedPrivacyPolicy,
    IReadOnlyList<string>? RequiredConsents = null
)
{
    public bool HasAllRequiredConsents => HasAcceptedTerms && HasAcceptedPrivacyPolicy;
}
