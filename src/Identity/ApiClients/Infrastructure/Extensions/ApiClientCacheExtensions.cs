
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Vitarish.ApiClients.Domain.Entities;

namespace Vitarish.ApiClients.Infrastructure.Extensions;

internal static class ApiClientCacheExtensions
{
    private const string ClientsKey = "api-clients:all";
    private static readonly DistributedCacheEntryOptions CacheOptions = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    internal static async Task<ApiClient[]?> GetClientsAsync(IDistributedCache cache, CancellationToken ct) =>
        Deserialize<ApiClient[]>(await cache.GetAsync(ClientsKey, ct));

    internal static Task SetClientsAsync(IDistributedCache cache, IReadOnlyCollection<ApiClient> clients, CancellationToken ct)
    {
        ApiClient[] payload = clients as ApiClient[] ?? clients.ToArray();
        return cache.SetAsync(ClientsKey, Serialize(payload), CacheOptions, ct);
    }

    internal static Task RemoveClientsAsync(IDistributedCache cache, CancellationToken ct) => cache.RemoveAsync(ClientsKey, ct);

    internal static async Task<ApiClient?> GetClientAsync(IDistributedCache cache, string id, CancellationToken ct) =>
        Deserialize<ApiClient>(await cache.GetAsync(ClientKey(id), ct));

    internal static Task SetClientAsync(IDistributedCache cache, ApiClient client, CancellationToken ct) =>
        cache.SetAsync(ClientKey(client.Id), Serialize(client), CacheOptions, ct);

    internal static Task RemoveClientAsync(IDistributedCache cache, string id, CancellationToken ct) =>
        cache.RemoveAsync(ClientKey(id), ct);

    private static string ClientKey(string id) => $"api-clients:{id}";

    private static byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

    private static T? Deserialize<T>(byte[]? payload) => payload is null ? default : JsonSerializer.Deserialize<T>(payload, JsonOptions);
}
