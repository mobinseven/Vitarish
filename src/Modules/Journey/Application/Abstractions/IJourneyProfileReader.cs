using Vitarish.Journey.Application.Models;

namespace Vitarish.Journey.Application.Abstractions;

public interface IJourneyProfileReader
{
    Task<JourneyProfileSnapshot> ReadAsync(string userId, CancellationToken cancellationToken);
}
