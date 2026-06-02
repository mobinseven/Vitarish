using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Users.Tests.TestHelpers;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class CreateUserWithEmailCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingEmail_ReturnsExistingUser()
    {
        User existing = new() { Id = "user-1", Email = "user@example.com", UserName = "user@example.com" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        _ = dbContext.Users.Add(existing);
        _ = await dbContext.SaveChangesAsync();

        UserManager<User> userManager = UserManagerFactory.Create(dbContext);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        CreateUserWithEmailCommandHandler handler = new(userManager, cache.Object);

        User? result = await handler.Handle(new CreateUserWithEmailCommand("user@example.com", "First", "Last"), CancellationToken.None);

        _ = result.Should().Be(existing);
        cache.Verify(c => c.RemoveAsync("users:all", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNewEmail_CreatesUser()
    {
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        UserManager<User> userManager = UserManagerFactory.Create(dbContext);
        Mock<IDistributedCache> cache = DistributedCacheMockFactory.Create();

        CreateUserWithEmailCommandHandler handler = new(userManager, cache.Object);

        User? result = await handler.Handle(new CreateUserWithEmailCommand("user@example.com", "First", "Last"), CancellationToken.None);

        _ = result.Should().NotBeNull();
        _ = result!.Email.Should().Be("user@example.com");
        _ = result.FirstName.Should().Be("First");
        _ = result.LastName.Should().Be("Last");
        User? created = await dbContext.Users.SingleOrDefaultAsync(u => u.Email == "user@example.com");
        _ = created.Should().NotBeNull();
        cache.Verify(c => c.SetAsync($"users:{created!.Id}", It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("users:all", It.IsAny<CancellationToken>()), Times.Once);
    }
}
