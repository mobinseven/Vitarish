
namespace Vitarish.Clients.Interface.Identity.Abstractions;

public interface IUserAccessClient
{
    Task<bool> CheckUserAccess(string resource, CancellationToken cancellationToken = default);
    Task<UserDto?> GetCurrentUser(CancellationToken cancellationToken = default);
    Task<IEnumerable<UserDto>> GetUsers(CancellationToken cancellationToken = default);
}
