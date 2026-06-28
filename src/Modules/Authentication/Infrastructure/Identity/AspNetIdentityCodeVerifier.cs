using Microsoft.AspNetCore.Identity;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AspNetIdentityCodeVerifier(UserManager<User> userManager) : IAuthenticationCodeVerifier
{
    public async Task<bool> VerifyAsync(string challengeId, string code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(challengeId) || string.IsNullOrWhiteSpace(code)) return false;
        string[] parts = challengeId.Split(':', 3);
        if (parts.Length != 3) return false;
        User? user = await userManager.FindByIdAsync(parts[1]);
        return user is not null && await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider, parts[0], code);
    }
}
