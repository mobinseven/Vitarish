using Vitarish.Journey.Application.Models;

namespace Vitarish.Journey.Application.Abstractions;

public interface IJourneyWorkspaceReader
{
    Task<JourneyWorkspaceSnapshot> ReadAsync(JourneyRequest request, CancellationToken cancellationToken);
}
