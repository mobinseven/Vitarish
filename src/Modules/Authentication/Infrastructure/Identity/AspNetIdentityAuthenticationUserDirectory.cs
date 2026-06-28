using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Infrastructure;

public sealed class AspNetIdentityAuthenticationUserDirectory(UserManager<User> userManager, IHttpContextAccessor httpContextAccessor) : IAuthenticationUserDirectory
{
    public async Task<AuthenticationUserDescriptor?> FindByLoginAsync(string login, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(login)) return null;
        User? user = await userManager.FindByNameAsync(login) ?? await userManager.FindByEmailAsync(login);
        return user is null ? null : ToDescriptor(user);
    }

    public async Task<AuthenticationUserDescriptor?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        User? user = await userManager.FindByEmailAsync(email);
        return user is null ? null : ToDescriptor(user);
    }

    public async Task<AuthenticationUserDescriptor?> FindByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return null;
        User? user = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber, cancellationToken);
        return user is null ? null : ToDescriptor(user);
    }

    public async Task<AuthenticationUserDescriptor?> GetCurrentAsync(CancellationToken cancellationToken)
    {
        ClaimsPrincipal? principal = httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true) return null;
        User? user = await userManager.GetUserAsync(principal);
        return user is null ? null : ToDescriptor(user);
    }

    private static AuthenticationUserDescriptor ToDescriptor(User user)
    {
        string? displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName : user.FullName;
        bool requiresProfileCompletion = string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName);
        return new AuthenticationUserDescriptor(user.Id, user.UserName, displayName, user.Email, user.PhoneNumber, requiresProfileCompletion);
    }
}
