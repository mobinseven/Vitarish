using Users.Tests.TestHelpers;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class GetUserProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserMissing_Throws()
    {
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        GetUserProfileQueryHandler handler = new(dbContext);

        Func<Task> act = () => handler.Handle(new GetUserProfileQuery("missing"), CancellationToken.None);

        _ = await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task Handle_ReturnsProfilePreferences()
    {
        User user = new()
        {
            Id = "user-1",
            ProfilePreferences = new Dictionary<string, string> { ["theme"] = "dark" }
        };

        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        _ = dbContext.Users.Add(user);
        _ = await dbContext.SaveChangesAsync();

        GetUserProfileQueryHandler handler = new(dbContext);

        Dictionary<string, string> result = await handler.Handle(new GetUserProfileQuery("user-1"), CancellationToken.None);

        _ = result.Should().ContainKey("theme").WhoseValue.Should().Be("dark");
    }
}
