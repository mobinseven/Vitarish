using Microsoft.AspNetCore.Identity;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AspNetIdentitySignInService(UserManager<User> userManager, SignInManager<User> signInManager) : IAuthenticationSignInService
{
    public async Task<bool> PasswordSignInAsync(string login, string password, bool rememberMe, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password)) return false;
        User? user = await userManager.FindByNameAsync(login) ?? await userManager.FindByEmailAsync(login);
        if (user is null) return false;
        SignInResult result = await signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);
        return result.Succeeded;
    }

    public async Task SignInAsync(string userId, bool rememberMe, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        User? user = await userManager.FindByIdAsync(userId);
        if (user is null) throw new InvalidOperationException("Cannot sign in an unknown user.");
        await signInManager.SignInAsync(user, rememberMe);
    }

    public Task SignOutAsync(CancellationToken cancellationToken) => signInManager.SignOutAsync();
}
