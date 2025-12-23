

namespace Vitarish.Clients.Interface.Identity.Abstractions;


public interface IUsersService
{
    IEnumerable<UserDto> Users { get; }
    UserDto? User { get; }

    Task<bool> CheckUserRole(string roleName, CancellationToken cancellationToken = default);
    Task Initialize(string[] roles);
}

