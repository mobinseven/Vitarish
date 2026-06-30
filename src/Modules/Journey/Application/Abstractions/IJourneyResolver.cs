using Vitarish.Journey.Domain;

namespace Vitarish.Journey.Application.Abstractions;

/// <summary>
/// Resolves the next valid destination for an authenticated user.
/// </summary>
public interface IJourneyResolver
{
    /// <summary>
    /// Resolves the journey step for the given request.
    /// </summary>
    Task<JourneyStep> ResolveAsync(JourneyRequest request, CancellationToken cancellationToken = default);
}
