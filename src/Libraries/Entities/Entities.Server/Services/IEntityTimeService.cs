namespace Vitarish.Entities.Server.Services;

public interface IEntityTimeService
{
    DateTimeOffset Now { get; }
    void Set(DateTimeOffset? now);
}

