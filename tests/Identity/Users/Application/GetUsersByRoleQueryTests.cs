using Vitarish.Users.Application.Queries;

namespace Vitarish.Users.Tests.Application;

public class GetUsersByRoleQueryTests
{
    [Fact]
    public void GetUsersByRoleQuery_StoresRoleName()
    {
        GetUsersByRoleQuery query = new("Admin");

        query.RoleName.Should().Be("Admin");
    }
}
