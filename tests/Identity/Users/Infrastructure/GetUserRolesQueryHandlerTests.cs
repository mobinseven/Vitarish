using Users.Tests.TestHelpers;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Infrastructure.Handlers;

namespace Users.Tests.Infrastructure;

public class GetUserRolesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsRolesForUser()
    {
        IdentityRole role = new() { Id = "role-1", Name = "tenant:Admin" };
        IdentityUserRole<string> userRole = new() { UserId = "user-1", RoleId = "role-1" };

        await using UsersDbContext dbContext = UsersDbContextFactory.Create();
        _ = dbContext.Roles.Add(role);
        _ = dbContext.UserRoles.Add(userRole);
        _ = await dbContext.SaveChangesAsync();

        GetUserRolesQueryHandler handler = new(dbContext);

        IEnumerable<IdentityRole> result = await handler.Handle(new GetUserRolesQuery("user-1"), CancellationToken.None);

        _ = result.Should().ContainSingle(r => r.Name == "tenant:Admin");
    }
}
