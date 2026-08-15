using Vitarish.Entities.Server.Abstractions;

namespace Vitarish.Main.Infrastructure.Entities;

internal sealed class UtcEntityTimeService : IEntityTimeService
{
    private DateTime? now;

    public DateTime Now => now ?? DateTime.UtcNow;

    public void Set(DateTime? now) => this.now = now switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } value => value,
        { Kind: DateTimeKind.Local } value => value.ToUniversalTime(),
        { } value => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
