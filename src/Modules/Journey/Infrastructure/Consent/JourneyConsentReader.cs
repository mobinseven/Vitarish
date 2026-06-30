using Vitarish.Journey.Application.Abstractions;
using Vitarish.Journey.Domain;

namespace Vitarish.Journey.Infrastructure.Consent;

/// <summary>
/// Reads required-consent state for journey evaluation.
/// Currently returns a default snapshot since full consent management is out of scope.
/// </summary>
public sealed class JourneyConsentReader : IJourneyConsentReader
{
    public Task<JourneyConsentSnapshot?> GetSnapshotAsync(string userId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement actual consent lookup when consent management is available
        // For now, return a snapshot that doesn't block the user
        return Task.FromResult<JourneyConsentSnapshot?>(new JourneyConsentSnapshot(
            HasAcceptedTerms: true,
            HasAcceptedPrivacyPolicy: true
        ));
    }
}
