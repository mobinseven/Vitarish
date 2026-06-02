using Users.Tests.TestHelpers;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class UserHasPasswordQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTrueWhenPasswordHashPresent()
    {
        User user = new() { Id = "user-1", PasswordHash = "hash" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        _ = dbContext.Users.Add(user);
        _ = await dbContext.SaveChangesAsync();

        UserHasPasswordQueryHandler handler = new(dbContext);

        bool result = await handler.Handle(new UserHasPasswordQuery("user-1"), CancellationToken.None);

        _ = result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsFalseWhenUserMissing()
    {
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        UserHasPasswordQueryHandler handler = new(dbContext);

        bool result = await handler.Handle(new UserHasPasswordQuery("missing"), CancellationToken.None);

        _ = result.Should().BeFalse();
    }
}
