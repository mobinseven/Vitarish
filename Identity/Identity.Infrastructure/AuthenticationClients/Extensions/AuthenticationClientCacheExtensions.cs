using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;

namespace Vitarish.Identity.Infrastructure.AuthenticationClients.Extensions;

internal static class AuthenticationClientCacheExtensions
{
    private const string ClientsKey = "authentication-clients:all";
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static async Task<AuthenticationClient[]?> GetClientsAsync(IDistributedCache cache, CancellationToken ct) =>
        Deserialize<AuthenticationClient[]>(await cache.GetAsync(ClientsKey, ct));

    internal static Task SetClientsAsync(IDistributedCache cache, IReadOnlyCollection<AuthenticationClient> clients, CancellationToken ct)
    {
        AuthenticationClient[] payload = clients is AuthenticationClient[] array ? array : clients.ToArray();
        return cache.SetAsync(ClientsKey, Serialize(payload), CacheOptions, ct);
    }

    internal static Task RemoveClientsAsync(IDistributedCache cache, CancellationToken ct) => cache.RemoveAsync(ClientsKey, ct);

    internal static async Task<AuthenticationClient?> GetClientAsync(IDistributedCache cache, string id, CancellationToken ct) =>
        Deserialize<AuthenticationClient>(await cache.GetAsync(ClientKey(id), ct));

    internal static Task SetClientAsync(IDistributedCache cache, AuthenticationClient client, CancellationToken ct) =>
        cache.SetAsync(ClientKey(client.Id), Serialize(client), CacheOptions, ct);

    internal static Task RemoveClientAsync(IDistributedCache cache, string id, CancellationToken ct) =>
        cache.RemoveAsync(ClientKey(id), ct);

    private static string ClientKey(string id) => $"authentication-clients:{id}";

    private static byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

    private static T? Deserialize<T>(byte[]? payload) => payload is null ? default : JsonSerializer.Deserialize<T>(payload, JsonOptions);
}
