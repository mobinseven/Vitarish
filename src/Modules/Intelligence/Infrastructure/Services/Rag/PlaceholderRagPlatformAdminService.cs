using Vitarish.Intelligence.Application.Abstractions;

namespace Vitarish.Intelligence.Infrastructure.Services.Rag;

public sealed class PlaceholderRagPlatformAdminService : IRagPlatformAdminService
{
    public Task<string> GetStatusAsync(CancellationToken cancellationToken = default)
        => Task.FromResult("PlatformAdmin is deferred to a future plan.");
}
