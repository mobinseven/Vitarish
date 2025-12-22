using Microsoft.AspNetCore.Components.Authorization;
using Vitarish.Clients.Interface.Identity.Extensions;

namespace Vitarish.Clients.Interface.Identity.Services;

public class UsersService(IUserAccessClient userAccessClient) : IUsersService
{
    public IEnumerable<UserDto> Users { get; private set; } = [];
    public UserDto? User { get; private set; }
    public virtual async Task Initialize()
    {
        Users = await userAccessClient.GetUsers();
        User = await userAccessClient.GetCurrentUser();
    }
}

