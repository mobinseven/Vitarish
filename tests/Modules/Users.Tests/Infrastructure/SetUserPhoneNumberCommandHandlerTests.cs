using Microsoft.Extensions.Caching.Distributed;
using Users.Tests.TestHelpers;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class SetUserPhoneNumberCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_ReturnsFailed()
    {
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        _ = userManager.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((User?)null);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        SetUserPhoneNumberCommandHandler handler = new(userManager.Object, cache.Object);

        IdentityResult result = await handler.Handle(new SetUserPhoneNumberCommand("missing", "123"), CancellationToken.None);

        _ = result.Succeeded.Should().BeFalse();
        _ = result.Errors.Should().ContainSingle(e => e.Description == "User not found");
    }

    [Fact]
    public async Task Handle_WhenPhoneSet_UpdatesCache()
    {
        User user = new() { Id = "user-1" };
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        _ = userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        _ = userManager.Setup(m => m.SetPhoneNumberAsync(user, "123")).ReturnsAsync(IdentityResult.Success);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        SetUserPhoneNumberCommandHandler handler = new(userManager.Object, cache.Object);

        IdentityResult result = await handler.Handle(new SetUserPhoneNumberCommand("user-1", "123"), CancellationToken.None);

        _ = result.Succeeded.Should().BeTrue();
        cache.Verify(c => c.SetAsync("users:user-1", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("users:all", It.IsAny<CancellationToken>()), Times.Once);
    }
}
