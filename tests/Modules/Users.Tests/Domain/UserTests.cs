using System.Text.Json;
using Vitarish.Users.Domain.Entities;

namespace Users.Tests.Domain;

public class UserTests
{
    [Fact]
    public void User_InitializesWithEmptyProfilePreferences()
    {
        User user = new();

        _ = user.ProfileJson.Should().BeNull();
        _ = user.ProfilePreferences.Should().BeEmpty();
        _ = user.FirstName.Should().BeNull();
        _ = user.LastName.Should().BeNull();
    }

    [Fact]
    public void User_ProfilePreferences_SerializesAndDeserializes()
    {
        User user = new();
        Dictionary<string, string> preferences = new()
        {
            ["theme"] = "dark",
            ["locale"] = "en-US"
        };

        user.ProfilePreferences = preferences;

        _ = user.ProfileJson.Should().NotBeNullOrWhiteSpace();
        _ = user.ProfilePreferences.Should().BeEquivalentTo(preferences);
        _ = JsonSerializer.Deserialize<Dictionary<string, string>>(user.ProfileJson!)
            .Should().BeEquivalentTo(preferences);
    }

    [Fact]
    public void User_ProfilePreferences_EmptyClearsProfileJson()
    {
        User user = new()
        {
            ProfilePreferences = new Dictionary<string, string> { ["pref"] = "value" }
        };

        user.ProfilePreferences = [];

        _ = user.ProfileJson.Should().BeNull();
        _ = user.ProfilePreferences.Should().BeEmpty();
    }
}
