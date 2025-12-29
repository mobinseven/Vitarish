using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Tests.Infrastructure;

public class UserCacheExtensionsTests
{
    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    [Fact]
    public async Task SetAndGetUserAsync_StoresUserByIdAndName()
    {
        IDistributedCache cache = CreateCache();
        User user = new() { Id = "user-1", UserName = "user1" };

        await UserCacheExtensions.SetUserAsync(cache, user, CancellationToken.None);

        User? byId = await UserCacheExtensions.GetUserAsync(cache, "user-1", CancellationToken.None);
        User? byName = await UserCacheExtensions.GetUserByNameAsync(cache, "user1", CancellationToken.None);

        byId.Should().NotBeNull();
        byId!.Id.Should().Be("user-1");
        byName.Should().NotBeNull();
        byName!.UserName.Should().Be("user1");
    }

    [Fact]
    public async Task SetAndGetUsersAsync_StoresCollection()
    {
        IDistributedCache cache = CreateCache();
        User[] users = [new User { Id = "user-1" }, new User { Id = "user-2" }];

        await UserCacheExtensions.SetUsersAsync(cache, users, CancellationToken.None);
        User[]? cached = await UserCacheExtensions.GetUsersAsync(cache, CancellationToken.None);

        cached.Should().NotBeNull();
        cached!.Select(u => u.Id).Should().BeEquivalentTo(["user-1", "user-2"]);
    }

    [Fact]
    public async Task RemoveUserAsync_ClearsUserAndUserList()
    {
        IDistributedCache cache = CreateCache();
        User user = new() { Id = "user-3", UserName = "user3" };
        await UserCacheExtensions.SetUserAsync(cache, user, CancellationToken.None);
        await UserCacheExtensions.SetUsersAsync(cache, [user], CancellationToken.None);

        await UserCacheExtensions.RemoveUserAsync(cache, user, CancellationToken.None);

        User? byId = await UserCacheExtensions.GetUserAsync(cache, "user-3", CancellationToken.None);
        User? byName = await UserCacheExtensions.GetUserByNameAsync(cache, "user3", CancellationToken.None);
        User[]? users = await UserCacheExtensions.GetUsersAsync(cache, CancellationToken.None);

        byId.Should().BeNull();
        byName.Should().BeNull();
        users.Should().BeNull();
    }
}
