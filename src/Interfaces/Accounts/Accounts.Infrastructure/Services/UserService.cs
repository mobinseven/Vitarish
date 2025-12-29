using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Vitarish.Users.Application.Commands;

public class UserService : IUserService
{
    private static class CacheKeys
    {
        public static string User(string userId) => $"User:{userId}";
        public static string Roles(string userId) => $"UserRoles:{userId}";
        public static string Profile(string userId) => $"UserProfile:{userId}";
        public static string Culture(string userId) => $"UserCulture:{userId}";
    }

    private const string CulturePreferenceKey = "Culture";

    private readonly IUsersService _usersService;
    private readonly IUserProfileService _profileService;
    private readonly IEmailAuthenticationService _emailAuthService;
    private readonly ISmsAuthenticationService _smsAuthService;
    private readonly IDistributedCache _cache;
    private readonly DistributedCacheEntryOptions _cacheOptions;
    private static readonly SemaphoreSlim _userLock = new SemaphoreSlim(1, 1); 

    public UserService(
        IUsersService usersService,
        IUserProfileService profileService,
        IEmailAuthenticationService emailAuthenticationService,
        ISmsAuthenticationService smsAuthService,
        IDistributedCache cache)
    {
        _usersService = usersService;
        _profileService = profileService;
        _emailAuthService = emailAuthenticationService;
        _smsAuthService = smsAuthService;
        _cache = cache;

        // Configure expiration as appropriate
        _cacheOptions = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(1),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(5)
        };
    }

    public async Task<User?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("userId is required", nameof(userId));
        }

        string cacheKey = CacheKeys.User(userId);

        // Try cache
        byte[]? cached = await _cache.GetAsync(cacheKey, ct);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<User>(cached);
        }

        await _userLock.WaitAsync(ct);
        try
        {
            // Cache miss
            User? user = await _usersService.GetUserAsync(userId, ct);
            if (user != null)
            {
                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(user);
                await _cache.SetAsync(cacheKey, bytes, _cacheOptions, ct);
            }
            return user;
        }
        finally
        {
            _userLock.Release();  // Release lock
        }
    }

    public async Task<IEnumerable<IdentityRole>> GetUserRolesAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("userId is required", nameof(userId));
        }

        string cacheKey = CacheKeys.Roles(userId);

        // Try cache
        byte[]? cached = await _cache.GetAsync(cacheKey, ct);
        if (cached != null)
        {
            // Deserialize to a concrete collection
            return JsonSerializer.Deserialize<IdentityRole[]>(cached) ?? Array.Empty<IdentityRole>();
        }

        // Cache miss
        IEnumerable<IdentityRole> roles = await _usersService.GetUserRolesAsync(userId, ct);

        // Materialize once (avoid re-enumeration) and cache even if empty
        IdentityRole[] rolesArray = roles as IdentityRole[] ?? roles.ToArray();

        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(rolesArray);
        await _cache.SetAsync(cacheKey, bytes, _cacheOptions, ct);

        return rolesArray;
    }

    public async Task<IdentityResult> UpdateUserAsync(string userId, User user, CancellationToken ct = default)
    {
        IdentityResult result = await _usersService.UpdateUserAsync(new UpdateUserCommand(user), ct);
        if (result.Succeeded)
        {
            // Invalidate cache
            await _cache.RemoveAsync(CacheKeys.User(userId), ct);
        }
        return result;
    }

    public async Task<Dictionary<string, string>?> GetProfileAsync(string userId)
    {
        string cacheKey = CacheKeys.Profile(userId);

        // Try cache
        byte[]? cached = await _cache.GetAsync(cacheKey);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(cached);
        }

        // Cache miss
        Dictionary<string, string>? prefs = await _profileService.GetProfileAsync(userId);
        if (prefs != null)
        {
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(prefs);
            await _cache.SetAsync(cacheKey, bytes, _cacheOptions);
        }
        return prefs;
    }

    public async Task UpdateProfileAsync(string userId, Dictionary<string, string> preferences)
    {
        await _profileService.UpdateProfileAsync(userId, preferences);

        // Update distributed cache
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(preferences);
        await _cache.SetAsync(CacheKeys.Profile(userId), bytes, _cacheOptions);
        // Also update derived culture
        if (preferences.TryGetValue(CulturePreferenceKey, out string? culture))
        {
            await _cache.SetStringAsync(CacheKeys.Culture(userId), culture, _cacheOptions);
        }
        else
        {
            await _cache.RemoveAsync(CacheKeys.Culture(userId));
        }
    }

    public async Task<string?> GetCultureAsync(string userId)
    {
        // Try cache first
        string? culture = await _cache.GetStringAsync(CacheKeys.Culture(userId));
        if (!string.IsNullOrEmpty(culture))
        {
            return culture;
        }

        // Cache miss or not present
        Dictionary<string, string>? prefs = await GetProfileAsync(userId);
        if (prefs == null)
        {
            return null;
        }

        string? result = GetCulture(prefs);
        if (result != null)
        {
            await _cache.SetStringAsync(CacheKeys.Culture(userId), result, _cacheOptions);
        }
        return result;
    }

    public async Task<bool> SetCultureAsync(string userId, string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return false;
        }

        Dictionary<string, string>? prefs = await GetProfileAsync(userId);
        if (prefs == null)
        {
            return false;
        }

        prefs[CulturePreferenceKey] = culture;

        await UpdateProfileAsync(userId, prefs);
        return true;
    }
    public Task<bool> UserHasPasswordAsync(string userId, CancellationToken ct = default) =>
        _usersService.UserHasPasswordAsync(userId, ct);

    public Task<bool> CheckUserEmailInUseAsync(string email, string? userId, CancellationToken ct = default) =>
        _usersService.CheckUserEmailInUseAsync(email, userId, ct);

    public Task<bool> CheckUserPhoneNumberInUseAsync(string phoneNumber, string? userId, CancellationToken ct = default) =>
        _usersService.CheckUserPhoneNumberInUseAsync(phoneNumber, userId, ct);

    public async Task SendEmailCode(string userId, string email)
    {
        User? user = await GetUserAsync(userId);
        await _emailAuthService.SendCode(user, email);
    }

    public async Task SendSmsCode(string userId, string phone)
    {
        User? user = await GetUserAsync(userId);
        await _smsAuthService.SendCode(user, phone);
    }

    public static string? GetCulture(IDictionary<string, string> preferences) =>
        preferences.TryGetValue(CulturePreferenceKey, out string? c) ? c : null;
}
