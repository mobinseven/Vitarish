
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Vitarish.AuthenticationClients.Domain.Entities;

namespace Vitarish.AuthenticationClients.Infrastructure.Extensions;

internal static class AuthenticationClientCacheExtensions
{
    private const string ClientsKey = "authentication-clients:all";
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static async Task<AuthenticationClient[]?> GetClientsAsync(IDistributedCache cache, CancellationToken ct) =>
        Deserialize<AuthenticationClient[]>(await cache.GetAsync(ClientsKey, ct));

    internal static Task SetClientsAsync(IDistributedCache cache, IReadOnlyCollection<AuthenticationClient> clients, CancellationToken ct)
    {
        AuthenticationClient[] payload = clients as AuthenticationClient[] ?? clients.ToArray();
        return cache.SetAsync(ClientsKey, Serialize(payload), CacheOptions, ct);
    }

    internal static Task RemoveClientsAsync(IDistributedCache cache, CancellationToken ct) => cache.RemoveAsync(ClientsKey, ct);

    internal static async Task<AuthenticationClient?> GetClientAsync(IDistributedCache cache, string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return Deserialize<AuthenticationClient>(await cache.GetAsync(ClientKey(id), ct));
    }

    internal static Task SetClientAsync(IDistributedCache cache, AuthenticationClient client, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(client.Id))
        {
            return Task.CompletedTask;
        }

        return cache.SetAsync(ClientKey(client.Id), Serialize(client), CacheOptions, ct);
    }

    internal static Task RemoveClientAsync(IDistributedCache cache, string id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return Task.CompletedTask;
        }

        return cache.RemoveAsync(ClientKey(id), ct);
    }

    private static string ClientKey(string id) => $"authentication-clients:{id}";

    private static byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

    private static T? Deserialize<T>(byte[]? payload) => payload is null ? default : JsonSerializer.Deserialize<T>(payload, JsonOptions);
}
