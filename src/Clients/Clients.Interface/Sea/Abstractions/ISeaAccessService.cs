namespace Vitarish.Clients.Interface.Sea.Abstractions;

public interface ISeaAccessService : IUsersService
{
    /// <summary>
    /// Indicates whether the current user has admin access within the Sea client.
    /// </summary>
    IReadOnlyList<Guid> RoleIds { get; }
    Guid? UserId { get; }
    Task<bool> CanReadEvent(Guid eventId);
    Task<bool> CanWriteEvent(Guid eventId);
    Task<IReadOnlyDictionary<Guid, (bool CanRead, bool CanWrite)>> GetEventAccessAsync(IEnumerable<Guid> eventIds);
}
