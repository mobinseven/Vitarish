using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Tests.TestHelpers;

namespace Vitarish.Users.Tests.Infrastructure;

public class DeleteUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_ReturnsFailed()
    {
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        userManager.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((User?)null);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        DeleteUserCommandHandler handler = new(userManager.Object, cache.Object);

        IdentityResult result = await handler.Handle(new DeleteUserCommand("missing"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Description == "User not found");
    }

    [Fact]
    public async Task Handle_WhenDeleteSucceeds_RemovesCache()
    {
        User user = new() { Id = "user-1" };
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);
        userManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        DeleteUserCommandHandler handler = new(userManager.Object, cache.Object);

        IdentityResult result = await handler.Handle(new DeleteUserCommand("user-1"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        cache.Verify(c => c.RemoveAsync("users:user-1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
