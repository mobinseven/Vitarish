using Vitarish.Users.Application.Queries;

namespace Users.Tests.Application;

public class GetUsersByRoleQueryTests
{
    [Fact]
    public void GetUsersByRoleQuery_StoresRoleName()
    {
        GetUsersByRoleQuery query = new("Admin");

        _ = query.RoleName.Should().Be("Admin");
    }
}
