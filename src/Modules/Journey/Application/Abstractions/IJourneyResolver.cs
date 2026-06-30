using Vitarish.Journey.Application.Models;
using Vitarish.Journey.Domain;

namespace Vitarish.Journey.Application.Abstractions;

public interface IJourneyResolver
{
    Task<JourneyDecision> ResolveAsync(JourneyRequest request, CancellationToken cancellationToken);
}
