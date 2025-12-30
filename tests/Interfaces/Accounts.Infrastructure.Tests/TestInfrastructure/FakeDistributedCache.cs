using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Vitarish.Accounts.Infrastructure.Tests.TestInfrastructure;

public sealed class FakeDistributedCache : IDistributedCache
{
    private readonly MemoryDistributedCache cache;

    public FakeDistributedCache()
    {
        cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    public byte[]? Get(string key) => cache.Get(key);

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        cache.GetAsync(key, token);

    public void Refresh(string key) => cache.Refresh(key);

    public Task RefreshAsync(string key, CancellationToken token = default) =>
        cache.RefreshAsync(key, token);

    public void Remove(string key) => cache.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default) =>
        cache.RemoveAsync(key, token);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        cache.Set(key, value, options);

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
        cache.SetAsync(key, value, options, token);
}
