using Vitarish.Users.Application.Queries;

namespace Users.Tests.Application;

public class GetUserRolesQueryTests
{
    [Fact]
    public void GetUserRolesQuery_StoresUserId()
    {
        GetUserRolesQuery query = new("user-1");

        query.UserId.Should().Be("user-1");
    }
}
