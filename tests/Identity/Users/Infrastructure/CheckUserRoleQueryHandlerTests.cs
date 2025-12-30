using Microsoft.AspNetCore.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Tests.TestHelpers;

namespace Vitarish.Users.Tests.Infrastructure;

public class CheckUserRoleQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenUserIdMissing_Throws()
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(r => r.UserId).Returns((string?)null);
        requestContext.SetupGet(r => r.TenantId).Returns("tenant-1");

        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        CheckUserRoleQueryHandler handler = new(dbContext, requestContext.Object);

        Func<Task> act = () => handler.Handle(new CheckUserRoleQuery("Admin"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task Handle_ReturnsTrueWhenRoleAssigned()
    {
        IdentityRole role = new() { Id = "role-1", Name = "tenant-1:Admin" };
        IdentityUserRole<string> userRole = new() { UserId = "user-1", RoleId = "role-1" };

        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        dbContext.Roles.Add(role);
        dbContext.UserRoles.Add(userRole);
        await dbContext.SaveChangesAsync();

        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(r => r.UserId).Returns("user-1");
        requestContext.SetupGet(r => r.TenantId).Returns("tenant-1");

        CheckUserRoleQueryHandler handler = new(dbContext, requestContext.Object);

        bool result = await handler.Handle(new CheckUserRoleQuery("Admin"), CancellationToken.None);

        result.Should().BeTrue();
    }
}
