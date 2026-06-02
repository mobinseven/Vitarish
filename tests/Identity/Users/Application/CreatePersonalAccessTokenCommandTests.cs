using Vitarish.Users.Application.Commands;

namespace Users.Tests.Application;

public class CreatePersonalAccessTokenCommandTests
{
    [Fact]
    public void CreatePersonalAccessTokenCommand_StoresValues()
    {
        CreatePersonalAccessTokenCommand command = new("user-1", 10, ["scope1"], true);

        _ = command.UserId.Should().Be("user-1");
        _ = command.LifetimeDays.Should().Be(10);
        _ = command.Scopes.Should().ContainSingle("scope1");
        _ = command.IsReferenceToken.Should().BeTrue();
    }

    [Fact]
    public void CreatePersonalAccessTokenCommand_DefaultsToNullLifetimeAndJwt()
    {
        CreatePersonalAccessTokenCommand command = new("user-2");

        _ = command.UserId.Should().Be("user-2");
        _ = command.LifetimeDays.Should().BeNull();
        _ = command.Scopes.Should().BeNull();
        _ = command.IsReferenceToken.Should().BeFalse();
    }
}
