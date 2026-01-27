using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Vitarish.Users.Application.Commands;

public class UserService : IUserService
{
    private static class Keys
    {
        public static string User => $"User";
        public static string Roles => $"UserRoles";
        public static string Profile => $"UserProfile";
        public static string Culture => $"UserCulture";
        public static string Theme => $"UserTheme";
    }
    private static class CacheKeys
    {
        public static string User(string userId) => $"{Keys.User}:{userId}";
        public static string Roles(string userId) => $"{Keys.Roles}:{userId}";
        public static string Profile(string userId) => $"{Keys.Profile}:{userId}";
        public static string Culture(string userId) => $"{Keys.Culture}:{userId}";
        public static string Theme(string userId) => $"{Keys.Theme}:{userId}";
    }

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

    public async Task<IdentityResult> CreateUserAsync(User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            User? created = await _usersService.CreateUserWithEmailAsync(
                new CreateUserWithEmailCommand(user.Email, user.FirstName, user.LastName),
                ct);
            return created is null
                ? IdentityResult.Failed(new IdentityError { Code = "CreateUserFailed", Description = "Unable to create user with email." })
                : UpdateUserId(user, created);
        }

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            User? created = await _usersService.CreateUserWithPhoneAsync(
                new CreateUserWithPhoneCommand(user.PhoneNumber, user.FirstName, user.LastName),
                ct);
            return created is null
                ? IdentityResult.Failed(new IdentityError { Code = "CreateUserFailed", Description = "Unable to create user with phone number." })
                : UpdateUserId(user, created);
        }

        return IdentityResult.Failed(new IdentityError { Code = "InvalidUser", Description = "Email or phone number is required." });
    }

    private static IdentityResult UpdateUserId(User user, User created)
    {
        user.Id = created.Id;
        return IdentityResult.Success;
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
        // Also update derived theme
        if (preferences.TryGetValue(Keys.Culture, out string? culture))
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

    public async Task<string?> GetThemeAsync(string userId)
    {
        // Try cache first
        string? theme = await _cache.GetStringAsync(CacheKeys.Theme(userId));
        if (!string.IsNullOrEmpty(theme))
        {
            return theme;
        }

        // Cache miss or not present
        Dictionary<string, string>? prefs = await GetProfileAsync(userId);
        if (prefs == null)
        {
            return null;
        }

        string? result = GetTheme(prefs);
        if (result != null)
        {
            await _cache.SetStringAsync(CacheKeys.Theme(userId), result, _cacheOptions);
        }
        return result;
    }

    public async Task SetCultureAsync(string userId, string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
        {
            return;
        }

        Dictionary<string, string>? prefs = await GetProfileAsync(userId);
        if (prefs == null)
        {
            return;
        }

        prefs[Keys.Culture] = culture;

        await UpdateProfileAsync(userId, prefs);
    }

    public async Task<bool> SetThemeAsync(string userId, string theme)
    {
        if (string.IsNullOrWhiteSpace(theme))
        {
            return false;
        }

        Dictionary<string, string>? prefs = await GetProfileAsync(userId);
        if (prefs == null)
        {
            return false;
        }

        prefs[Keys.Theme] = theme;

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
        preferences.TryGetValue(Keys.Culture, out string? c) ? c : null;
    public static string? GetTheme(IDictionary<string, string> preferences) =>
        preferences.TryGetValue(Keys.Theme, out string? c) ? c : null;
}
