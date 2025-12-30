using Vitarish.Users.Application.Queries;

namespace Vitarish.Users.Tests.Application;

public class GetCurrentUserQueryTests
{
    [Fact]
    public void GetCurrentUserQuery_CanBeConstructed()
    {
        GetCurrentUserQuery query = new();

        query.Should().NotBeNull();
    }
}
