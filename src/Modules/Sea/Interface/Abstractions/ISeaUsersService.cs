using Vitarish.Users.Domain.Entities;

namespace Vitarish.Sea.Interface.Abstractions;

public interface ISeaUsersService
{
    /// <summary>
    /// Indicates whether the current user has admin access within the Sea client.
    /// </summary>
    IReadOnlyList<Guid> RoleIds { get; }
    Guid? UserId { get; }
    UserDto? User { get; }
    IEnumerable<UserDto> Users { get; }

    Task<bool> CanReadEvent(Guid eventId);
    Task<bool> CanWriteEvent(Guid eventId);
    Task<IReadOnlyDictionary<Guid, (bool CanRead, bool CanWrite)>> GetEventAccessAsync(IEnumerable<Guid> eventIds);
}
