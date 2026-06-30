using Vitarish.Journey.Application.Models;

namespace Vitarish.Journey.Application.Abstractions;

public interface IJourneyConsentReader
{
    Task<JourneyConsentSnapshot> ReadAsync(string userId, CancellationToken cancellationToken);
}
