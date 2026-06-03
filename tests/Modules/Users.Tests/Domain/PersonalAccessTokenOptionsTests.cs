using Vitarish.Users.Domain.Options;

namespace Users.Tests.Domain;

public class PersonalAccessTokenOptionsTests
{
    [Fact]
    public void PersonalAccessTokenOptions_DefaultLifetimeIs30Days()
    {
        PersonalAccessTokenOptions options = new();

        _ = options.DefaultLifetimeDays.Should().Be(30);
    }

    [Fact]
    public void PersonalAccessTokenOptions_AllowsCustomLifetime()
    {
        PersonalAccessTokenOptions options = new() { DefaultLifetimeDays = 7 };

        _ = options.DefaultLifetimeDays.Should().Be(7);
    }
}
