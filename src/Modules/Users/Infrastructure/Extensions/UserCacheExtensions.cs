
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Extensions;

internal static class UserCacheExtensions
{
    private const string UsersKey = "users:all";
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static async Task<User[]?> GetUsersAsync(IDistributedCache cache, CancellationToken ct) =>
        Deserialize<User[]>(await cache.GetAsync(UsersKey, ct));

    internal static Task SetUsersAsync(IDistributedCache cache, IReadOnlyCollection<User> users, CancellationToken ct)
    {
        User[] payload = users as User[] ?? [.. users];
        return cache.SetAsync(UsersKey, Serialize(payload), CacheOptions, ct);
    }

    internal static Task RemoveUsersAsync(IDistributedCache cache, CancellationToken ct) => cache.RemoveAsync(UsersKey, ct);

    internal static async Task<User?> GetUserAsync(IDistributedCache cache, string id, CancellationToken ct) =>
        Deserialize<User>(await cache.GetAsync(UserKey(id), ct));

    internal static async Task<User?> GetUserByNormalizedUserNameAsync(IDistributedCache cache, string normalizedUserName, CancellationToken ct) =>
        Deserialize<User>(await cache.GetAsync(NormalizedUserNameKey(normalizedUserName), ct));

    internal static Task SetUserAsync(IDistributedCache cache, User user, CancellationToken ct)
    {
        byte[] payload = Serialize(user);
        Task setById = cache.SetAsync(UserKey(user.Id), payload, CacheOptions, ct);
        Task setByNormalizedName = string.IsNullOrWhiteSpace(user.NormalizedUserName)
            ? Task.CompletedTask
            : cache.SetAsync(NormalizedUserNameKey(user.NormalizedUserName), payload, CacheOptions, ct);
        return Task.WhenAll(setById, setByNormalizedName);
    }

    internal static Task RemoveUserAsync(IDistributedCache cache, User user, CancellationToken ct)
    {
        Task removeById = cache.RemoveAsync(UserKey(user.Id), ct);
        Task removeByName = string.IsNullOrWhiteSpace(user.NormalizedUserName)
            ? Task.CompletedTask
            : cache.RemoveAsync(NormalizedUserNameKey(user.NormalizedUserName), ct);
        Task removeUsers = cache.RemoveAsync(UsersKey, ct);
        return Task.WhenAll(removeById, removeByName, removeUsers);
    }

    private static string UserKey(string id) => $"users:{id}";

    private static string NormalizedUserNameKey(string normalizedUserName) => $"users:normalized-name:{normalizedUserName}";

    private static byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

    private static T? Deserialize<T>(byte[]? payload) => payload is null ? default : JsonSerializer.Deserialize<T>(payload, JsonOptions);
}
