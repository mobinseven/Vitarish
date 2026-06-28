using Microsoft.AspNetCore.Identity;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AspNetIdentityCodeGenerator(UserManager<User> userManager) : IAuthenticationCodeGenerator
{
    public async Task<string> GenerateAsync(string userId, string tokenPurpose, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User ID is required.", nameof(userId));

        User? user = await userManager.FindByIdAsync(userId);
        if (user is null) throw new InvalidOperationException("User not found.");

        return await userManager.GenerateUserTokenAsync(user, TokenOptions.DefaultProvider, tokenPurpose);
    }
}
