using Vitarish.Users.Application.Commands;

namespace Vitarish.Accounts.Infrastructure.Services;

public class UserService(IUsersService usersService,
    IUserProfileService profileService,
    IEmailAuthenticationService emailAuthenticationService,
    ISmsAuthenticationService smsAuthenticationService) : IUserService
{
    private const string CulturePreferenceKey = "Culture";
    public User? CurrentUser { get; set; }
    public string? CurrentUserId { get; set; }
    public async Task<User?> GetUserAsync(string? userId = null, CancellationToken ct = default)
    {
        if (userId == null && CurrentUserId == null)
        {
            throw new InvalidOperationException("Current user ID must be set at this stage.");
        }
        CurrentUserId ??= userId;
        CurrentUser ??= await usersService.GetUserAsync(CurrentUserId, ct);
        return CurrentUser;
    }

    public async Task<IdentityResult> UpdateUserAsync(User user, CancellationToken ct = default)
    {
        var res = await usersService.UpdateUserAsync(new UpdateUserCommand(user), ct);
        if (res.Succeeded)
        {
            CurrentUser = await usersService.GetUserAsync(CurrentUserId, ct);
        }
        return res;
    }
    public Task<bool> UserHasPasswordAsync(CancellationToken ct = default) =>
        usersService.UserHasPasswordAsync(CurrentUserId, ct);

    public Task<bool> CheckUserEmailInUseAsync(string email, CancellationToken ct = default) =>
        usersService.CheckUserEmailInUseAsync(email, CurrentUserId, ct);

    public Task<bool> CheckUserPhoneNumberInUseAsync(string phoneNumber, CancellationToken ct = default) =>
        usersService.CheckUserPhoneNumberInUseAsync(phoneNumber, CurrentUserId, ct);


    public async Task SendEmailCode(string email)
    {
        _ = await GetUserAsync();
        await emailAuthenticationService.SendCode(CurrentUser, email);
    }

    public async Task SendSmsCode(string phone)
    {
        _ = await GetUserAsync();
        await smsAuthenticationService.SendCode(CurrentUser, phone);
    }

    public async Task<Dictionary<string, string>?> GetProfileAsync()
    {
        _ = await GetUserAsync();
        return CurrentUser?.ProfilePreferences;
    }

    public async Task UpdateProfileAsync(Dictionary<string, string> preferences) => await profileService.UpdateProfileAsync(CurrentUserId, preferences);

    public static string? GetCulture(IDictionary<string, string> preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        return preferences.TryGetValue(CulturePreferenceKey, out string? culture) ? culture : null;
    }

    public void SetCulture(IDictionary<string, string> preferences, string culture)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        if (string.IsNullOrWhiteSpace(culture))
        {
            return;
        }

        preferences[CulturePreferenceKey] = culture;
    }

    public async Task<string?> GetCultureAsync()
    {
        Dictionary<string, string>? preferences = await GetProfileAsync();
        return preferences is null ? null : GetCulture(preferences);
    }

    public async Task<bool> SetCultureAsync(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return false;
        }

        Dictionary<string, string>? preferences = await GetProfileAsync();
        if (preferences is null)
        {
            return false;
        }

        SetCulture(preferences, culture);
        await UpdateProfileAsync(preferences);
        return true;
    }
}
