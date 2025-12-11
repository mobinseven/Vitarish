using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Vitarish.AuthenticationClients.Tests.TestHelpers;

/// <summary>
/// A simple in-memory distributed cache for testing purposes.
/// </summary>
public sealed class FakeDistributedCache : IDistributedCache
{
    private readonly MemoryDistributedCache _cache;

    public FakeDistributedCache()
    {
        var options = Options.Create(new MemoryDistributedCacheOptions());
        _cache = new MemoryDistributedCache(options);
    }

    public byte[]? Get(string key) => _cache.Get(key);

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        _cache.GetAsync(key, token);

    public void Refresh(string key) => _cache.Refresh(key);

    public Task RefreshAsync(string key, CancellationToken token = default) =>
        _cache.RefreshAsync(key, token);

    public void Remove(string key) => _cache.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default) =>
        _cache.RemoveAsync(key, token);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        _cache.Set(key, value, options);

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
        _cache.SetAsync(key, value, options, token);
}
