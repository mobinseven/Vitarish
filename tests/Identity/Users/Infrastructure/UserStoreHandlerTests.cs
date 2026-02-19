using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Users.Tests.TestHelpers;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class CreateUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserCreated_UpdatesCache()
    {
        User user = new() { Id = "user-1", UserName = "user-1" };
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        userManager.Setup(m => m.CreateAsync(user)).ReturnsAsync(IdentityResult.Success);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        CreateUserCommandHandler handler = new(userManager.Object, cache.Object);

        IdentityResult result = await handler.Handle(new CreateUserCommand(user), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        cache.Verify(c => c.SetAsync("users:user-1", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("users:all", It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class SetUserSecurityStampCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenUpdated_StoresSecurityStampAndCaches()
    {
        User user = new() { Id = "user-1" };
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        SetUserSecurityStampCommandHandler handler = new(userManager.Object, cache.Object);

        IdentityResult result = await handler.Handle(new SetUserSecurityStampCommand(user, "stamp"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        user.SecurityStamp.Should().Be("stamp");
        cache.Verify(c => c.SetAsync("users:user-1", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("users:all", It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class AddUserToRoleCommandHandlerTests
{
    [Fact]
    public async Task Handle_AddsRoleWithTenantPrefix()
    {
        User user = new() { Id = "user-1" };
        Mock<UserManager<User>> userManager = UserManagerMockFactory.Create();
        userManager.Setup(m => m.AddToRoleAsync(user, "tenant-1:admin")).ReturnsAsync(IdentityResult.Success);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(r => r.TenantId).Returns("tenant-1");

        AddUserToRoleCommandHandler handler = new(userManager.Object, requestContext.Object);

        IdentityResult result = await handler.Handle(new AddUserToRoleCommand(user, "admin"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        userManager.Verify(m => m.AddToRoleAsync(user, "tenant-1:admin"), Times.Once);
    }
}

public class GetUserByNormalizedNameQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsUserByNormalizedName()
    {
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        User user = new()
        {
            Id = "user-1",
            UserName = "someone",
            NormalizedUserName = "SOMEONE"
        };
        dbContext.Users.Add(user);
        _ = await dbContext.SaveChangesAsync();
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        GetUserByNormalizedNameQueryHandler handler = new(dbContext, cache.Object);

        User? result = await handler.Handle(new GetUserByNormalizedNameQuery("SOMEONE"), CancellationToken.None);

        result.Should().NotBeNull();
        cache.Verify(c => c.SetAsync("users:user-1", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
