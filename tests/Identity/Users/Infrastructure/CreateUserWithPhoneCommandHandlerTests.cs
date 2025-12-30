using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Tests.TestHelpers;

namespace Vitarish.Users.Tests.Infrastructure;

public class CreateUserWithPhoneCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingPhone_ReturnsExistingUser()
    {
        User existing = new() { Id = "user-1", PhoneNumber = "5551234", UserName = "5551234" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        dbContext.Users.Add(existing);
        await dbContext.SaveChangesAsync();

        UserManager<User> userManager = UserManagerFactory.Create(dbContext);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        CreateUserWithPhoneCommandHandler handler = new(userManager, cache.Object);

        User? result = await handler.Handle(new CreateUserWithPhoneCommand("5551234", "First", "Last"), CancellationToken.None);

        result.Should().Be(existing);
        cache.Verify(c => c.RemoveAsync("users:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNewPhone_CreatesUser()
    {
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        UserManager<User> userManager = UserManagerFactory.Create(dbContext);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        CreateUserWithPhoneCommandHandler handler = new(userManager, cache.Object);

        User? result = await handler.Handle(new CreateUserWithPhoneCommand("5551234", "First", "Last"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.PhoneNumber.Should().Be("5551234");
        result.FirstName.Should().Be("First");
        result.LastName.Should().Be("Last");
        User? created = await dbContext.Users.SingleOrDefaultAsync(u => u.PhoneNumber == "5551234");
        created.Should().NotBeNull();
        cache.Verify(c => c.SetAsync($"users:{created!.Id}", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("users:all", It.IsAny<CancellationToken>()), Times.Once);
    }
}
