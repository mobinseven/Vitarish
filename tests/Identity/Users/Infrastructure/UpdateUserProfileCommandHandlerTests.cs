using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Tests.TestHelpers;

namespace Vitarish.Users.Tests.Infrastructure;

public class UpdateUserProfileCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserExists_UpdatesProfileAndCache()
    {
        User user = new() { Id = "user-1" };
        Dictionary<string, string> preferences = new() { ["theme"] = "dark" };
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        UpdateUserProfileCommandHandler handler = new(userManager.Object, cache.Object);

        await handler.Handle(new UpdateUserProfileCommand("user-1", preferences), CancellationToken.None);

        user.ProfilePreferences.Should().BeEquivalentTo(preferences);
        cache.Verify(c => c.SetAsync("users:user-1", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("users:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserMissing_Throws()
    {
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        userManager.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((User?)null);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        UpdateUserProfileCommandHandler handler = new(userManager.Object, cache.Object);

        Func<Task> act = () => handler.Handle(new UpdateUserProfileCommand("missing", new Dictionary<string, string>()), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDataException>();
    }
}
