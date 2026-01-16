using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Tests.TestHelpers;

namespace Users.Tests.Infrastructure;

public class GetUsersByRoleQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsUsersMatchingTenantRole()
    {
        User user = new() { Id = "user-1", UserName = "user1" };
        IdentityRole role = new() { Id = "role-1", Name = "tenant-1:Admin" };
        IdentityUserRole<string> userRole = new() { UserId = "user-1", RoleId = "role-1" };

        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        _ = dbContext.Users.Add(user);
        _ = dbContext.Roles.Add(role);
        _ = dbContext.UserRoles.Add(userRole);
        _ = await dbContext.SaveChangesAsync();

        GetUsersByRoleQueryHandler handler = new(dbContext);

        IEnumerable<User> result = await handler.Handle(new GetUsersByRoleQuery("Admin"), CancellationToken.None);

        _ = result.Should().ContainSingle(u => u.Id == "user-1");
    }

    [Fact]
    public async Task Handle_WhenNoMatch_ReturnsEmpty()
    {
        await using UsersDbContext dbContext = UsersDbContextFactory.Create();

        GetUsersByRoleQueryHandler handler = new(dbContext);

        IEnumerable<User> result = await handler.Handle(new GetUsersByRoleQuery("Admin"), CancellationToken.None);

        _ = result.Should().BeEmpty();
    }
}
