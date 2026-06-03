using Users.Tests.TestHelpers;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class CheckUserPasswordQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_ReturnsFalse()
    {
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        Mock<IPasswordHasher<User>> hasher = new();

        CheckUserPasswordQueryHandler handler = new(dbContext, hasher.Object);

        bool result = await handler.Handle(new CheckUserPasswordQuery("missing", "password"), CancellationToken.None);

        _ = result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenNoPasswordHash_ReturnsFalse()
    {
        User user = new() { Id = "user-1" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        _ = dbContext.Users.Add(user);
        _ = await dbContext.SaveChangesAsync();

        Mock<IPasswordHasher<User>> hasher = new();
        CheckUserPasswordQueryHandler handler = new(dbContext, hasher.Object);

        bool result = await handler.Handle(new CheckUserPasswordQuery("user-1", "password"), CancellationToken.None);

        _ = result.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenPasswordValid_ReturnsTrue()
    {
        User user = new() { Id = "user-1", PasswordHash = "hash" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        _ = dbContext.Users.Add(user);
        _ = await dbContext.SaveChangesAsync();

        Mock<IPasswordHasher<User>> hasher = new();
        _ = hasher.Setup(h => h.VerifyHashedPassword(It.IsAny<User>(), "hash", "password"))
            .Returns(PasswordVerificationResult.Success);

        CheckUserPasswordQueryHandler handler = new(dbContext, hasher.Object);

        bool result = await handler.Handle(new CheckUserPasswordQuery("user-1", "password"), CancellationToken.None);

        _ = result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenPasswordInvalid_ReturnsFalse()
    {
        User user = new() { Id = "user-1", PasswordHash = "hash" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        _ = dbContext.Users.Add(user);
        _ = await dbContext.SaveChangesAsync();

        Mock<IPasswordHasher<User>> hasher = new();
        _ = hasher.Setup(h => h.VerifyHashedPassword(user, "hash", "wrong"))
            .Returns(PasswordVerificationResult.Failed);

        CheckUserPasswordQueryHandler handler = new(dbContext, hasher.Object);

        bool result = await handler.Handle(new CheckUserPasswordQuery("user-1", "wrong"), CancellationToken.None);

        _ = result.Should().BeFalse();
    }
}
