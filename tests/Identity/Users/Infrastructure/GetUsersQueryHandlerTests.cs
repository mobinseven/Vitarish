using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Users.Tests.TestHelpers;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class GetUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCached_ReturnsCachedUsers()
    {
        IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        User[] cached = [new User { Id = "user-1" }];
        await UserCacheExtensions.SetUsersAsync(cache, cached, CancellationToken.None);

        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        GetUsersQueryHandler handler = new(dbContext, cache);

        IEnumerable<User> result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(cached);
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ReturnsUsersAndCaches()
    {
        User[] users = [new User { Id = "user-1" }, new User { Id = "user-2" }];
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        dbContext.Users.AddRange(users);
        await dbContext.SaveChangesAsync();

        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();
        GetUsersQueryHandler handler = new(dbContext, cache.Object);

        IEnumerable<User> result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        cache.Verify(c => c.SetAsync("users:all", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
