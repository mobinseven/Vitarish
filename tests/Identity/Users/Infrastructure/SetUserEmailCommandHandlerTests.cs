using Microsoft.Extensions.Caching.Distributed;
using Users.Tests.TestHelpers;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class SetUserEmailCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_ReturnsFailed()
    {
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        _ = userManager.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((User?)null);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        SetUserEmailCommandHandler handler = new(userManager.Object, cache.Object);

        IdentityResult result = await handler.Handle(new SetUserEmailCommand("missing", "user@example.com"), CancellationToken.None);

        _ = result.Succeeded.Should().BeFalse();
        _ = result.Errors.Should().ContainSingle(e => e.Description == "User not found");
    }

    [Fact]
    public async Task Handle_WhenEmailSet_UpdatesCache()
    {
        User user = new() { Id = "user-1" };
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        _ = userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        _ = userManager.Setup(m => m.SetEmailAsync(user, "user@example.com")).ReturnsAsync(IdentityResult.Success);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        SetUserEmailCommandHandler handler = new(userManager.Object, cache.Object);

        IdentityResult result = await handler.Handle(new SetUserEmailCommand("user-1", "user@example.com"), CancellationToken.None);

        _ = result.Succeeded.Should().BeTrue();
        cache.Verify(c => c.SetAsync("users:user-1", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("users:all", It.IsAny<CancellationToken>()), Times.Once);
    }
}
