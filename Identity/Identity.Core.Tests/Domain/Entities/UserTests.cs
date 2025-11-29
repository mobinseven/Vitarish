namespace Vitarish.Identity.Core.Tests.Domain.Entities;

using System.Text.Json;
using Vitarish.Identity.Core.Domain.Entities;

public class UserTests
{
    [Fact]
    public void User_NewInstance_HasNullablePropertiesNull()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        Assert.Null(user.FirstName);
        Assert.Null(user.LastName);
        Assert.Null(user.ProfileJson);
    }

    [Fact]
    public void User_SetFirstNameAndLastName_FullNameIsCorrect()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Assert
        Assert.Equal("John Doe", user.FullName);
    }

    [Fact]
    public void User_OnlyFirstName_FullNameIsTrimmed()
    {
        // Arrange
        var user = new User { FirstName = "Jane" };

        // Assert
        Assert.Equal("Jane", user.FullName);
    }

    [Fact]
    public void User_OnlyLastName_FullNameIsTrimmed()
    {
        // Arrange
        var user = new User { LastName = "Smith" };

        // Assert
        Assert.Equal("Smith", user.FullName);
    }

    [Fact]
    public void User_NoName_FullNameIsEmpty()
    {
        // Arrange
        var user = new User();

        // Assert
        Assert.Equal("", user.FullName);
    }

    [Fact]
    public void User_ProfilePreferences_SerializesToProfileJson()
    {
        // Arrange
        var user = new User();
        var preferences = new Dictionary<string, string>
        {
            ["theme"] = "dark",
            ["language"] = "en"
        };

        // Act
        user.ProfilePreferences = preferences;

        // Assert
        Assert.NotNull(user.ProfileJson);
        Assert.Contains("theme", user.ProfileJson);
        Assert.Contains("dark", user.ProfileJson);
    }

    [Fact]
    public void User_ProfilePreferences_DeserializesFromProfileJson()
    {
        // Arrange
        var user = new User
        {
            ProfileJson = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["setting1"] = "value1"
            })
        };

        // Act
        var preferences = user.ProfilePreferences;

        // Assert
        Assert.Equal("value1", preferences["setting1"]);
    }

    [Fact]
    public void User_EmptyProfilePreferences_SetsProfileJsonNull()
    {
        // Arrange
        var user = new User
        {
            ProfileJson = JsonSerializer.Serialize(new Dictionary<string, string> { ["key"] = "val" })
        };

        // Act
        user.ProfilePreferences = new Dictionary<string, string>();

        // Assert
        Assert.Null(user.ProfileJson);
    }

    [Fact]
    public void User_NullProfilePreferences_SetsProfileJsonNull()
    {
        // Arrange
        var user = new User
        {
            ProfileJson = JsonSerializer.Serialize(new Dictionary<string, string> { ["key"] = "val" })
        };

        // Act
        user.ProfilePreferences = null!;

        // Assert
        Assert.Null(user.ProfileJson);
    }

    [Fact]
    public void User_EmptyProfileJson_ReturnsEmptyPreferences()
    {
        // Arrange
        var user = new User { ProfileJson = null };

        // Act
        var preferences = user.ProfilePreferences;

        // Assert
        Assert.Empty(preferences);
    }

    [Fact]
    public void User_InheritsFromIdentityUser()
    {
        // Arrange
        var user = new User
        {
            UserName = "testuser",
            Email = "test@example.com",
            PhoneNumber = "+1234567890"
        };

        // Assert
        Assert.Equal("testuser", user.UserName);
        Assert.Equal("test@example.com", user.Email);
        Assert.Equal("+1234567890", user.PhoneNumber);
    }
}
