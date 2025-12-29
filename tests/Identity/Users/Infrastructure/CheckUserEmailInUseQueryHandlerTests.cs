using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Tests.TestHelpers;

namespace Vitarish.Users.Tests.Infrastructure;

public class CheckUserEmailInUseQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsTrueWhenEmailInUse()
    {
        User user = new() { Id = "user-1", NormalizedEmail = "USER@EXAMPLE.COM" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        Mock<ILookupNormalizer> normalizer = new();
        normalizer.Setup(n => n.NormalizeEmail("user@example.com")).Returns("USER@EXAMPLE.COM");

        CheckUserEmailInUseQueryHandler handler = new(dbContext, normalizer.Object);

        bool result = await handler.Handle(new CheckUserEmailInUseQuery("user@example.com", null), CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RespectsExcludeUserId()
    {
        User user = new() { Id = "user-1", NormalizedEmail = "USER@EXAMPLE.COM" };
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        Mock<ILookupNormalizer> normalizer = new();
        normalizer.Setup(n => n.NormalizeEmail("user@example.com")).Returns("USER@EXAMPLE.COM");

        CheckUserEmailInUseQueryHandler handler = new(dbContext, normalizer.Object);

        bool result = await handler.Handle(new CheckUserEmailInUseQuery("user@example.com", "user-1"), CancellationToken.None);

        result.Should().BeFalse();
    }
}
