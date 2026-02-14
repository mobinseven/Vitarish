namespace Vitarish.Entities.Server.Abstractions;

public interface IEntityTimeService
{
    DateTimeOffset Now { get; }
    void Set(DateTimeOffset? now);
}

