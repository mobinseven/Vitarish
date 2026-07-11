using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Main.Infrastructure.Entities;

internal sealed class UtcEntityTimeService : IEntityTimeService
{
    private DateTime? now;

    public DateTime Now => now ?? DateTime.UtcNow;

    public void Set(DateTime? now) => this.now = now?.Kind switch
    {
        null => null,
        DateTimeKind.Utc => now.Value,
        DateTimeKind.Local => now.Value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(now.Value, DateTimeKind.Utc)
    };
}
