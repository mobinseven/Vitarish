using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Accounts.Application.Abstractions;

public interface IUserService
{
    User? CurrentUser { get; set; }
    string? CurrentUserId { get; set; }

    Task<bool> CheckUserEmailInUseAsync(string email, CancellationToken ct = default);
    Task<bool> CheckUserPhoneNumberInUseAsync(string phoneNumber, CancellationToken ct = default);
    Task<string?> GetCultureAsync();
    Task<Dictionary<string, string>?> GetProfileAsync();
    Task<User?> GetUserAsync(string? userId = null, CancellationToken ct = default);
    Task SendEmailCode(string email);
    Task SendSmsCode(string phone);
    Task<bool> SetCultureAsync(string culture);
    Task UpdateProfileAsync(Dictionary<string, string> preferences);
    Task<IdentityResult> UpdateUserAsync(User user, CancellationToken ct = default);
    Task<bool> UserHasPasswordAsync(CancellationToken ct = default);
}
