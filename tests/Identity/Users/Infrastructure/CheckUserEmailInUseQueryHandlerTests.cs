using Users.Tests.TestHelpers;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class CheckUserEmailInUseQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTrueWhenEmailInUse()
    {
        User user = new() { Id = "user-1", NormalizedEmail = "USER@EXAMPLE.COM" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        _ = dbContext.Users.Add(user);
        _ = await dbContext.SaveChangesAsync();

        Mock<ILookupNormalizer> normalizer = new();
        _ = normalizer.Setup(n => n.NormalizeEmail("user@example.com")).Returns("USER@EXAMPLE.COM");

        CheckUserEmailInUseQueryHandler handler = new(dbContext, normalizer.Object);

        bool result = await handler.Handle(new CheckUserEmailInUseQuery("user@example.com", null), CancellationToken.None);

        _ = result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RespectsExcludeUserId()
    {
        User user = new() { Id = "user-1", NormalizedEmail = "USER@EXAMPLE.COM" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        _ = dbContext.Users.Add(user);
        _ = await dbContext.SaveChangesAsync();

        Mock<ILookupNormalizer> normalizer = new();
        _ = normalizer.Setup(n => n.NormalizeEmail("user@example.com")).Returns("USER@EXAMPLE.COM");

        CheckUserEmailInUseQueryHandler handler = new(dbContext, normalizer.Object);

        bool result = await handler.Handle(new CheckUserEmailInUseQuery("user@example.com", "user-1"), CancellationToken.None);

        _ = result.Should().BeFalse();
    }
}
