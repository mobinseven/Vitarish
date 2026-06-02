using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Users.Tests.TestHelpers;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class GetUserByNameQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCached_ReturnsCachedUser()
    {
        IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        User cached = new() { Id = "user-1", UserName = "user1" };
        await UserCacheExtensions.SetUserAsync(cache, cached, CancellationToken.None);

        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        GetUserByNameQueryHandler handler = new(dbContext, cache);

        User? result = await handler.Handle(new GetUserByNameQuery("user1"), CancellationToken.None);

        _ = result.Should().NotBeNull();
        _ = result!.Id.Should().Be("user-1");
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ReturnsUserAndCaches()
    {
        User user = new() { Id = "user-2", UserName = "user2" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        _ = dbContext.Users.Add(user);
        _ = await dbContext.SaveChangesAsync();

        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();
        GetUserByNameQueryHandler handler = new(dbContext, cache.Object);

        User? result = await handler.Handle(new GetUserByNameQuery("user2"), CancellationToken.None);

        _ = result.Should().NotBeNull();
        cache.Verify(c => c.SetAsync("users:user-2", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
