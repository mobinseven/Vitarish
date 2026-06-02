using Microsoft.Extensions.Caching.Distributed;
using Users.Tests.TestHelpers;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class UpdateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUpdateSucceeds_UpdatesCache()
    {
        User user = new() { Id = "user-1" };
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        _ = userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        UpdateUserCommandHandler handler = new(userManager.Object, cache.Object);

        IdentityResult result = await handler.Handle(new UpdateUserCommand(user), CancellationToken.None);

        _ = result.Succeeded.Should().BeTrue();
        cache.Verify(c => c.SetAsync("users:user-1", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("users:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUpdateFails_DoesNotUpdateCache()
    {
        User user = new() { Id = "user-1" };
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        _ = userManager.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "fail" }));
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        UpdateUserCommandHandler handler = new(userManager.Object, cache.Object);

        IdentityResult result = await handler.Handle(new UpdateUserCommand(user), CancellationToken.None);

        _ = result.Succeeded.Should().BeFalse();
        cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
