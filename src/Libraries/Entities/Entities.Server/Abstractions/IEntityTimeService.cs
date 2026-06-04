namespace Vitarish.Entities.Server.Abstractions;

public interface IEntityTimeService
{
    DateTime Now { get; }
    void Set(DateTime? now);
}

