using Microsoft.Extensions.Caching.Distributed;

namespace Users.Tests.TestHelpers;

internal static class DistributedCacheMockFactory
{
    internal static Mock<IDistributedCache> Create()
    {
        Mock<IDistributedCache> cache = new();
        cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);
        return cache;
    }
}
