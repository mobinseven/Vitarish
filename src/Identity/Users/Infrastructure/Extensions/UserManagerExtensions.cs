using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Extensions;

internal static class UserManagerExtensions
{
    public static async Task<User> CreateUserWithoutPasswordAsync(
        this UserManager<User> userManager,
        string userName,
        string? firstName,
        string? lastName,
        Func<User, Task> setUserContact)
    {
        User user = Activator.CreateInstance<User>();
        _ = await userManager.SetUserNameAsync(user, userName);
        await setUserContact(user);
        user.FirstName = firstName;
        user.LastName = lastName;
        IdentityResult result = await userManager.CreateAsync(user);
        return !result.Succeeded ? throw new Exception(string.Join('\n', result.Errors.Select(e => $"{e.Code}:{e.Description}"))) : user;
    }
}
