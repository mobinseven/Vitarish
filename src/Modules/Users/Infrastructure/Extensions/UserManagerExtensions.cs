using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Application.Common;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Extensions;

internal static class UserManagerExtensions
{
    public static async Task<User> CreateUserWithoutPasswordAsync(
        this UserManager<User> userManager,
        string userName,
        string? firstName,
        string? lastName,
        Func<User, Task> setUserContact,
        string? identificationCode = null,
        DateOnly? dateOfBirth = null,
        string? birthCountryCode = null,
        string? birthCity = null)
    {
        User user = Activator.CreateInstance<User>();
        _ = await userManager.SetUserNameAsync(user, userName);
        await setUserContact(user);
        user.FirstName = firstName;
        user.LastName = lastName;
        user.IdentificationCode = UserIdentityNormalization.NormalizeIdentificationCode(identificationCode);
        user.DateOfBirth = dateOfBirth;
        user.BirthCountryCode = birthCountryCode?.Trim();
        user.BirthCity = birthCity?.Trim();
        IdentityResult result = await userManager.CreateAsync(user);
        return !result.Succeeded ? throw new CreateUserFailureException(result) : user;
    }
}
