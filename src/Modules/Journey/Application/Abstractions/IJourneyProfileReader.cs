using Vitarish.Journey.Domain;

namespace Vitarish.Journey.Application.Abstractions;

/// <summary>
/// Reads profile/contact completion state for journey evaluation.
/// </summary>
public interface IJourneyProfileReader
{
    /// <summary>
    /// Gets a snapshot of the user's profile state.
    /// </summary>
    Task<JourneyProfileSnapshot?> GetSnapshotAsync(string userId, CancellationToken cancellationToken = default);
}
