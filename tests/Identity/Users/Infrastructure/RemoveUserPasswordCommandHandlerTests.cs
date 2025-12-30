using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Tests.TestHelpers;

namespace Vitarish.Users.Tests.Infrastructure;

public class RemoveUserPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_ReturnsFailed()
    {
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        userManager.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((User?)null);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        RemoveUserPasswordCommandHandler handler = new(userManager.Object, cache.Object);

        IdentityResult result = await handler.Handle(new RemoveUserPasswordCommand("missing"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Description == "User not found");
    }

    [Fact]
    public async Task Handle_WhenPasswordRemoved_UpdatesCache()
    {
        User user = new() { Id = "user-1" };
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        userManager.Setup(m => m.RemovePasswordAsync(user)).ReturnsAsync(IdentityResult.Success);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        RemoveUserPasswordCommandHandler handler = new(userManager.Object, cache.Object);

        IdentityResult result = await handler.Handle(new RemoveUserPasswordCommand("user-1"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        cache.Verify(c => c.SetAsync("users:user-1", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("users:all", It.IsAny<CancellationToken>()), Times.Once);
    }
}
