namespace Vitarish.Clients.Interface.Sea.Abstractions;

public interface ISeaAccessService : IAccessService
{
    IReadOnlyList<Guid> RoleIds { get; }
    Guid? UserId { get; }
    Task<bool> CanReadEvent(Guid eventId);
    Task<bool> CanWriteEvent(Guid eventId);
}

