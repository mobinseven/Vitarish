using Vitarish.Users.Domain.Events;

namespace Vitarish.Users.Tests.Domain;

public class UserAccessClaimRemovedEventTests
{
    [Fact]
    public void UserAccessClaimRemovedEvent_StoresPayload()
    {
        UserAccessClaimRemovedEvent @event = new("user-1", "client-2");

        @event.UserId.Should().Be("user-1");
        @event.ClientId.Should().Be("client-2");
    }
}
