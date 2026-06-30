using Vitarish.Journey.Domain;

namespace Vitarish.Journey.Application.Abstractions;

/// <summary>
/// Reads required-consent state for journey evaluation.
/// </summary>
public interface IJourneyConsentReader
{
    /// <summary>
    /// Gets a snapshot of the user's consent state.
    /// </summary>
    Task<JourneyConsentSnapshot?> GetSnapshotAsync(string userId, CancellationToken cancellationToken = default);
}
