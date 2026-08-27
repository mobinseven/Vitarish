using Microsoft.AspNetCore.Identity;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AspNetIdentitySignInService(UserManager<User> userManager, SignInManager<User> signInManager) : IAuthenticationSignInService
{
    public async Task<PasswordSignInResult> PasswordSignInAsync(string login, string password, bool rememberMe, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return new PasswordSignInResult(PasswordSignInStatus.InvalidCredentials, null, false);

        User? user = await userManager.FindByNameAsync(login) ?? await userManager.FindByEmailAsync(login);
        if (user is null)
            return new PasswordSignInResult(PasswordSignInStatus.UserNotFound, null, false);

        SignInResult result = await signInManager.PasswordSignInAsync(user, password, rememberMe, lockoutOnFailure: true);

        PasswordSignInStatus status = result.Succeeded
            ? PasswordSignInStatus.Succeeded
            : result.IsLockedOut
                ? PasswordSignInStatus.LockedOut
                : result.IsNotAllowed
                    ? PasswordSignInStatus.NotAllowed
                    : result.RequiresTwoFactor
                        ? PasswordSignInStatus.RequiresTwoFactor
                        : PasswordSignInStatus.InvalidCredentials;

        bool requiresProfileCompletion = string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName);

        return new PasswordSignInResult(status, user.Id, requiresProfileCompletion, status == PasswordSignInStatus.RequiresTwoFactor && user.TwoFactorEnabled);
    }

    public async Task<PasswordSignInResult> CheckPasswordSignInAsync(string login, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            return new PasswordSignInResult(PasswordSignInStatus.InvalidCredentials, null, false);

        User? user = await userManager.FindByNameAsync(login) ?? await userManager.FindByEmailAsync(login);
        if (user is null)
            return new PasswordSignInResult(PasswordSignInStatus.UserNotFound, null, false);

        SignInResult result = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        PasswordSignInStatus status = result.Succeeded
            ? PasswordSignInStatus.Succeeded
            : result.IsLockedOut
                ? PasswordSignInStatus.LockedOut
                : result.IsNotAllowed
                    ? PasswordSignInStatus.NotAllowed
                    : result.RequiresTwoFactor
                        ? PasswordSignInStatus.RequiresTwoFactor
                        : PasswordSignInStatus.InvalidCredentials;

        bool requiresProfileCompletion = string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName);
        return new PasswordSignInResult(status, user.Id, requiresProfileCompletion, (status is PasswordSignInStatus.Succeeded or PasswordSignInStatus.RequiresTwoFactor) && user.TwoFactorEnabled);
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
