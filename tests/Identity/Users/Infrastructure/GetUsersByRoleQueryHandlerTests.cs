using Microsoft.AspNetCore.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Tests.TestHelpers;

namespace Vitarish.Users.Tests.Infrastructure;

public class GetUsersByRoleQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsUsersMatchingTenantRole()
    {
        User user = new() { Id = "user-1", UserName = "user1" };
        IdentityRole role = new() { Id = "role-1", Name = "tenant-1:Admin" };
        IdentityUserRole<string> userRole = new() { UserId = "user-1", RoleId = "role-1" };

        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.UserRoles.Add(userRole);
        await dbContext.SaveChangesAsync();

        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(r => r.TenantId).Returns("tenant-1");

        GetUsersByRoleQueryHandler handler = new(dbContext, requestContext.Object);

        IEnumerable<User> result = await handler.Handle(new GetUsersByRoleQuery("Admin"), CancellationToken.None);

        result.Should().ContainSingle(u => u.Id == "user-1");
    }

    [Fact]
    public async Task Handle_WhenNoMatch_ReturnsEmpty()
    {
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(r => r.TenantId).Returns("tenant-2");

        GetUsersByRoleQueryHandler handler = new(dbContext, requestContext.Object);

        IEnumerable<User> result = await handler.Handle(new GetUsersByRoleQuery("Admin"), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
