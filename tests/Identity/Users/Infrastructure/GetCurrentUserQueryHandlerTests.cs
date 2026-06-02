using Users.Tests.TestHelpers;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class GetCurrentUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserIdNull_Throws()
    {
        Mock<IRequestContext> requestContext = new();
        _ = requestContext.SetupGet(r => r.UserId).Returns((string?)null);

        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        GetCurrentUserQueryHandler handler = new(dbContext, requestContext.Object);

        Func<Task> act = () => handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        _ = await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task Handle_ReturnsCurrentUser()
    {
        User user = new() { Id = "user-1", UserName = "user1" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        _ = dbContext.Users.Add(user);
        _ = await dbContext.SaveChangesAsync();

        Mock<IRequestContext> requestContext = new();
        _ = requestContext.SetupGet(r => r.UserId).Returns("user-1");

        GetCurrentUserQueryHandler handler = new(dbContext, requestContext.Object);

        User? result = await handler.Handle(new GetCurrentUserQuery(), CancellationToken.None);

        _ = result.Should().NotBeNull();
        _ = result!.Id.Should().Be("user-1");
    }
}
