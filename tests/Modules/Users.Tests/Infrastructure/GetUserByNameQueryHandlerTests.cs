using Microsoft.AspNetCore.Identity;
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
    private static readonly ILookupNormalizer LookupNormalizer = new UpperInvariantLookupNormalizer();

    [Fact]
    public async Task Handle_WhenCached_ReturnsCachedUserByNormalizedUserName()
    {
        IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        User cached = new() { Id = "user-1", UserName = "Example.User", NormalizedUserName = "EXAMPLE.USER" };
        await UserCacheExtensions.SetUserAsync(cache, cached, CancellationToken.None);

        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        GetUserByNameQueryHandler handler = new(dbContext, cache, LookupNormalizer);

        User? result = await handler.Handle(new GetUserByNameQuery("example.user"), CancellationToken.None);

        _ = result.Should().NotBeNull();
        _ = result!.Id.Should().Be("user-1");
    }

    [Theory]
    [InlineData("Example.User")]
    [InlineData("example.user")]
    [InlineData("EXAMPLE.USER")]
    public async Task Handle_WhenCacheMiss_ReturnsUserByNormalizedUserName(string loginUserName)
    {
        User user = new() { Id = "user-2", UserName = "Example.User", NormalizedUserName = "EXAMPLE.USER" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        _ = dbContext.Users.Add(user);
        _ = await dbContext.SaveChangesAsync();

        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();
        GetUserByNameQueryHandler handler = new(dbContext, cache.Object, LookupNormalizer);

        User? result = await handler.Handle(new GetUserByNameQuery(loginUserName), CancellationToken.None);

        _ = result.Should().NotBeNull();
        _ = result!.Id.Should().Be("user-2");
        cache.Verify(c => c.SetAsync("users:user-2", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.SetAsync("users:normalized-name:EXAMPLE.USER", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.SetAsync("users:name:Example.User", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenUserNameIsEmpty_Throws(string loginUserName)
    {
        IDistributedCache cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        GetUserByNameQueryHandler handler = new(dbContext, cache, LookupNormalizer);

        Func<Task> act = () => handler.Handle(new GetUserByNameQuery(loginUserName), CancellationToken.None);

        _ = await act.Should().ThrowAsync<ArgumentException>();
    }
}
