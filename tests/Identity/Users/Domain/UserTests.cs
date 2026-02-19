using System.Text.Json;
using Vitarish.Users.Domain.Entities;

namespace Users.Tests.Domain;

public class UserTests
{
    [Fact]
    public void User_InitializesWithEmptyProfilePreferences()
    {
        User user = new();

        user.ProfileJson.Should().BeNull();
        user.ProfilePreferences.Should().BeEmpty();
        user.FirstName.Should().BeNull();
        user.LastName.Should().BeNull();
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

        user.ProfileJson.Should().NotBeNullOrWhiteSpace();
        user.ProfilePreferences.Should().BeEquivalentTo(preferences);
        JsonSerializer.Deserialize<Dictionary<string, string>>(user.ProfileJson!)
            .Should().BeEquivalentTo(preferences);
    }

    [Fact]
    public void User_ProfilePreferences_EmptyClearsProfileJson()
    {
        User user = new();
        user.ProfilePreferences = new Dictionary<string, string> { ["pref"] = "value" };

        user.ProfilePreferences = [];

        user.ProfileJson.Should().BeNull();
        user.ProfilePreferences.Should().BeEmpty();
    }
}
