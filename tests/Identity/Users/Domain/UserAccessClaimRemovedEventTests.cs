using Vitarish.Users.Domain.Events;

namespace Users.Tests.Domain;

public class UserAccessClaimRemovedEventTests
{
    [Fact]
    public void UserAccessClaimRemovedEvent_StoresPayload()
    {
        UserAccessClaimRemovedEvent @event = new("user-1", "client-2");

        _ = @event.UserId.Should().Be("user-1");
        _ = @event.ClientId.Should().Be("client-2");
    }
}
