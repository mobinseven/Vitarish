using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Tests.TestHelpers;

namespace Vitarish.Users.Tests.Infrastructure;

public class CheckUserPhoneNumberInUseQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTrueWhenPhoneInUse()
    {
        User user = new() { Id = "user-1", PhoneNumber = "123" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        CheckUserPhoneNumberInUseQueryHandler handler = new(dbContext);

        bool result = await handler.Handle(new CheckUserPhoneNumberInUseQuery("123", null), CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RespectsExcludeUserId()
    {
        User user = new() { Id = "user-1", PhoneNumber = "123" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        CheckUserPhoneNumberInUseQueryHandler handler = new(dbContext);

        bool result = await handler.Handle(new CheckUserPhoneNumberInUseQuery("123", "user-1"), CancellationToken.None);

        result.Should().BeFalse();
    }
}
