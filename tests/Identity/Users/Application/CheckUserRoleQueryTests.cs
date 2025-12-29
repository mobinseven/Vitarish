using Vitarish.Users.Application.Queries;

namespace Vitarish.Users.Tests.Application;

public class CheckUserRoleQueryTests
{
    [Fact]
    public void CheckUserRoleQuery_StoresRoleName()
    {
        CheckUserRoleQuery query = new("Admin");

        query.RoleName.Should().Be("Admin");
    }
}
