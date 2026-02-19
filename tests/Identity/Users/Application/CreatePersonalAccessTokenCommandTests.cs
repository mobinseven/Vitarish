using Vitarish.Users.Application.Commands;

namespace Users.Tests.Application;

public class CreatePersonalAccessTokenCommandTests
{
    [Fact]
    public void CreatePersonalAccessTokenCommand_StoresValues()
    {
        CreatePersonalAccessTokenCommand command = new("user-1", 10, ["scope1"], true);

        command.UserId.Should().Be("user-1");
        command.LifetimeDays.Should().Be(10);
        command.Scopes.Should().ContainSingle("scope1");
        command.IsReferenceToken.Should().BeTrue();
    }

    [Fact]
    public void CreatePersonalAccessTokenCommand_DefaultsToNullLifetimeAndJwt()
    {
        CreatePersonalAccessTokenCommand command = new("user-2");

        command.UserId.Should().Be("user-2");
        command.LifetimeDays.Should().BeNull();
        command.Scopes.Should().BeNull();
        command.IsReferenceToken.Should().BeFalse();
    }
}
