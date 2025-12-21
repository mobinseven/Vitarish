namespace Vitarish.Clients.Interface.Sea.Abstractions;

public interface ISeaAccessService : IAccessService
{
    bool IsAdmin { get; }
    IReadOnlyList<Guid> RoleIds { get; }
    Guid? UserId { get; }
    Task<bool> CanReadEvent(Guid eventId);
    Task<bool> CanWriteEvent(Guid eventId);
    Task<IReadOnlyDictionary<Guid, (bool CanRead, bool CanWrite)>> GetEventAccessAsync(IEnumerable<Guid> eventIds);
}
