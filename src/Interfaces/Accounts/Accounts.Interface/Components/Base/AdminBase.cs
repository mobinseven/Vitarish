using Vitarish.Parties.Application.Abstractions;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Accounts.Interface.Components.Base
{
    public class AdminBase : IdentityCommonBase
    {
        [Inject] protected IPartiesService PartiesService { get; set; } = default!;
        [Inject] protected IUsersService UsersService { get; set; } = default!;
        [Inject] protected UserManager<User> UserManager { get; set; } = default!;
        [Inject] protected RoleManager<IdentityRole> RoleManager { get; set; } = default!;
        [Inject] protected UsersDbContext UsersDbContext { get; set; } = default!;

    }
}
