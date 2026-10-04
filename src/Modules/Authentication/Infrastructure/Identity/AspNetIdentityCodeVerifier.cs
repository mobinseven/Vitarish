using Microsoft.AspNetCore.Identity;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AspNetIdentityCodeVerifier(UserManager<User> userManager) : IAuthenticationCodeVerifier
{
    public async Task<bool> VerifyAsync(string userId, string tokenPurpose, string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(tokenPurpose) || string.IsNullOrWhiteSpace(code))
            return false;

        User? user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultEmailProvider, tokenPurpose, code);
    }
}
