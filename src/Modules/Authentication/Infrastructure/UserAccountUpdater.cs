using Microsoft.AspNetCore.Identity;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Infrastructure;

public sealed class UserAccountUpdater(
    UserManager<User> userManager,
    IUsersService usersService) : IUserAccountUpdater
{
    public async Task<bool> SetPhoneNumberAsync(string userId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;

        IdentityResult result = await userManager.SetPhoneNumberAsync(user, phoneNumber);
        return result.Succeeded;
    }

    public async Task<bool> SetEmailAsync(string userId, string email, CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;

        IdentityResult result = await userManager.SetEmailAsync(user, email);
        return result.Succeeded;
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;

        IdentityResult result;
        if (string.IsNullOrEmpty(currentPassword))
        {
            string token = await userManager.GeneratePasswordResetTokenAsync(user);
            result = await userManager.ResetPasswordAsync(user, token, newPassword);
        }
        else
        {
            result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        }
        return result.Succeeded;
    }

    public async Task<bool> UserHasPasswordAsync(string userId, CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByIdAsync(userId);
        if (user is null) return false;
        return await userManager.HasPasswordAsync(user);
    }
}
