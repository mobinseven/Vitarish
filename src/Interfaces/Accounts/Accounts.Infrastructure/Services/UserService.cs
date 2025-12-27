using Vitarish.Users.Application.Commands;

namespace Vitarish.Accounts.Infrastructure.Services;

public class UserService(IUsersService usersService,
    IRequestContext requestContext,
    IUserProfileService profileService,
    IEmailAuthenticationService emailAuthenticationService,
    ISmsAuthenticationService smsAuthenticationService) : IUserService
{
    private const string CulturePreferenceKey = "Culture";
    public User? CurrentUser { get; set; }
    private string currentUserId => requestContext.UserId ?? throw new InvalidOperationException("Request context must supply user ID");
    public async Task<User?> GetUserAsync(CancellationToken ct = default) =>
        CurrentUser ??= await usersService.GetUserAsync(currentUserId, ct);
    public async Task<IdentityResult> UpdateUserAsync(User user, CancellationToken ct = default)
    {
        var res = await usersService.UpdateUserAsync(new UpdateUserCommand(user), ct);
        if (res.Succeeded)
        {
            CurrentUser = await usersService.GetUserAsync(currentUserId, ct);
        }
        return res;
    }
    public Task<bool> UserHasPasswordAsync(CancellationToken ct = default) =>
        usersService.UserHasPasswordAsync(currentUserId, ct);

    public Task<bool> CheckUserEmailInUseAsync(string email, CancellationToken ct = default) =>
        usersService.CheckUserEmailInUseAsync(email, currentUserId, ct);

    public Task<bool> CheckUserPhoneNumberInUseAsync(string phoneNumber, CancellationToken ct = default) =>
        usersService.CheckUserPhoneNumberInUseAsync(phoneNumber, currentUserId, ct);


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

    public async Task UpdateProfileAsync(Dictionary<string, string> preferences) => await profileService.UpdateProfileAsync(currentUserId, preferences);

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
