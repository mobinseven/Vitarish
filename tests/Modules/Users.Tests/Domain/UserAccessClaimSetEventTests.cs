using Vitarish.Users.Domain.Events;

namespace Users.Tests.Domain;

public class UserAccessClaimSetEventTests
{
    [Fact]
    public void UserAccessClaimSetEvent_StoresPayload()
    {
        UserAccessClaimSetEvent @event = new("user-1", "client-1", "scope.read");

        _ = @event.UserId.Should().Be("user-1");
        _ = @event.ClientId.Should().Be("client-1");
        _ = @event.AccessValue.Should().Be("scope.read");
    }
}
