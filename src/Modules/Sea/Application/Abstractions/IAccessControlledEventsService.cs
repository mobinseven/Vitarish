
namespace Vitarish.Sea.Application.Abstractions;

public interface IAccessControlledEventsService : IEventsService
{
    Task<bool> CanReadEvent(Guid eventId);
    Task<bool> CanWriteEvent(Guid eventId);
    Task CommitAsync(Guid eventId, CancellationToken cancellationToken);
}
