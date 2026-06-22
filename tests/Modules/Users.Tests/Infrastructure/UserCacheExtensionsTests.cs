using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;

namespace Users.Tests.Infrastructure;

public class UserCacheExtensionsTests
{
    private static IDistributedCache CreateCache() => new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));

    [Fact]
    public async Task SetAndGetUserAsync_StoresUserByIdAndNormalizedUserName()
    {
        IDistributedCache cache = CreateCache();
        User user = new() { Id = "user-1", UserName = "user1", NormalizedUserName = "USER1" };

        await UserCacheExtensions.SetUserAsync(cache, user, CancellationToken.None);

        Task<User?> byIdTask = UserCacheExtensions.GetUserAsync(cache, "user-1", CancellationToken.None);
        Task<User?> byNameTask = UserCacheExtensions.GetUserByNormalizedUserNameAsync(cache, "USER1", CancellationToken.None);
        _ = await Task.WhenAll(byIdTask, byNameTask);
        User? byId = await byIdTask;
        User? byName = await byNameTask;

        _ = byId.Should().NotBeNull();
        _ = byId!.Id.Should().Be("user-1");
        _ = byName.Should().NotBeNull();
        _ = byName!.UserName.Should().Be("user1");
    }

    [Fact]
    public async Task SetAndGetUsersAsync_StoresCollection()
    {
        IDistributedCache cache = CreateCache();
        User[] users = [new User { Id = "user-1" }, new User { Id = "user-2" }];

        await UserCacheExtensions.SetUsersAsync(cache, users, CancellationToken.None);
        User[]? cached = await UserCacheExtensions.GetUsersAsync(cache, CancellationToken.None);

        _ = cached.Should().NotBeNull();
        _ = cached!.Select(u => u.Id).Should().BeEquivalentTo(["user-1", "user-2"]);
    }

    [Fact]
    public async Task RemoveUserAsync_ClearsUserAndUserList()
    {
        IDistributedCache cache = CreateCache();
        User user = new() { Id = "user-3", UserName = "user3", NormalizedUserName = "USER3" };
        await UserCacheExtensions.SetUserAsync(cache, user, CancellationToken.None);
        await UserCacheExtensions.SetUsersAsync(cache, [user], CancellationToken.None);

        await UserCacheExtensions.RemoveUserAsync(cache, user, CancellationToken.None);

        User? byId = await UserCacheExtensions.GetUserAsync(cache, "user-3", CancellationToken.None);
        User? byName = await UserCacheExtensions.GetUserByNormalizedUserNameAsync(cache, "USER3", CancellationToken.None);
        User[]? users = await UserCacheExtensions.GetUsersAsync(cache, CancellationToken.None);

        _ = byId.Should().BeNull();
        _ = byName.Should().BeNull();
        _ = users.Should().BeNull();
    }
}
