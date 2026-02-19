using Vitarish.Users.Application.Queries;

namespace Users.Tests.Application;

public class GetCurrentUserQueryTests
{
    [Fact]
    public void GetCurrentUserQuery_CanBeConstructed()
    {
        GetCurrentUserQuery query = new();

        query.Should().NotBeNull();
    }
}
