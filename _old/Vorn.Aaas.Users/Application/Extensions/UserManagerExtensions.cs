namespace Vorn.Aaas.Users.Application.Extensions;

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Vorn.Aaas.Users.Domain.Entities;

internal static class UserManagerExtensions
{
    public static async Task<User> CreateUserWithoutPasswordAsync(
        this UserManager<User> userManager,
        string userName,
        string? firstName,
        string? lastName,
        Func<User, Task> setUserContact)
    {
        var user = Activator.CreateInstance<User>();
        await userManager.SetUserNameAsync(user, userName);
        await setUserContact(user);
        user.FirstName = firstName;
        user.LastName = lastName;
        IdentityResult result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            throw new Exception(string.Join('\n', result.Errors.Select(e => $"{e.Code}:{e.Description}")));
        }
        return user;
    }
}
