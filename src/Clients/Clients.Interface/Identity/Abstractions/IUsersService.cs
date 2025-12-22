
namespace Vitarish.Clients.Interface.Identity.Abstractions;


public interface IUsersService
{
    IEnumerable<UserDto> Users { get; }
    UserDto? User { get; }

    Task Initialize(string[] roles);
}

