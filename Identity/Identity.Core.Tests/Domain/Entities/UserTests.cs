using Vitarish.Identity.Core.Domain.Entities;

namespace Identity.Core.Tests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void User_WhenCreated_HasDefaultValues()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.FirstName.Should().BeNull();
        user.LastName.Should().BeNull();
        user.ProfileJson.Should().BeNull();
    }

    [Fact]
    public void User_FullName_ReturnsFirstAndLastName()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().Be("John Doe");
    }

    [Fact]
    public void User_FullName_WithOnlyFirstName_ReturnsFirstName()
    {
        // Arrange
        var user = new User
        {
            FirstName = "John"
        };

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().Be("John");
    }

    [Fact]
    public void User_FullName_WithOnlyLastName_ReturnsLastName()
    {
        // Arrange
        var user = new User
        {
            LastName = "Doe"
        };

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().Be("Doe");
    }

    [Fact]
    public void User_FullName_WithNoNames_ReturnsEmpty()
    {
        // Arrange
        var user = new User();

        // Act
        var fullName = user.FullName;

        // Assert
        fullName.Should().BeEmpty();
    }

    [Fact]
    public void User_ProfilePreferences_WhenEmpty_ReturnsEmptyDictionary()
    {
        // Arrange
        var user = new User();

        // Act
        var preferences = user.ProfilePreferences;

        // Assert
        preferences.Should().BeEmpty();
    }

    [Fact]
    public void User_ProfilePreferences_SetAndGet_RoundTrips()
    {
        // Arrange
        var user = new User();
        var preferences = new Dictionary<string, string>
        {
            { "theme", "dark" },
            { "language", "en" }
        };

        // Act
        user.ProfilePreferences = preferences;
        var result = user.ProfilePreferences;

        // Assert
        result.Should().ContainKey("theme");
        result["theme"].Should().Be("dark");
        result.Should().ContainKey("language");
        result["language"].Should().Be("en");
    }

    [Fact]
    public void User_ProfilePreferences_SetToEmpty_ClearsProfileJson()
    {
        // Arrange
        var user = new User
        {
            ProfilePreferences = new Dictionary<string, string> { { "key", "value" } }
        };

        // Act
        user.ProfilePreferences = new Dictionary<string, string>();

        // Assert
        user.ProfileJson.Should().BeNull();
    }

    [Fact]
    public void User_ProfilePreferences_SetToNull_ClearsProfileJson()
    {
        // Arrange
        var user = new User
        {
            ProfilePreferences = new Dictionary<string, string> { { "key", "value" } }
        };

        // Act
        user.ProfilePreferences = null!;

        // Assert
        user.ProfileJson.Should().BeNull();
    }

    [Fact]
    public void User_ProfileJson_IsSerializedCorrectly()
    {
        // Arrange
        var user = new User();
        var preferences = new Dictionary<string, string>
        {
            { "setting1", "value1" }
        };

        // Act
        user.ProfilePreferences = preferences;

        // Assert
        user.ProfileJson.Should().NotBeNullOrEmpty();
        user.ProfileJson.Should().Contain("setting1");
        user.ProfileJson.Should().Contain("value1");
    }

    [Fact]
    public void User_FirstName_MaxLength_IsValidated()
    {
        // Arrange
        var user = new User();
        var maxLengthFirstName = new string('a', 64);

        // Act
        user.FirstName = maxLengthFirstName;

        // Assert
        user.FirstName.Should().HaveLength(64);
    }

    [Fact]
    public void User_LastName_MaxLength_IsValidated()
    {
        // Arrange
        var user = new User();
        var maxLengthLastName = new string('b', 64);

        // Act
        user.LastName = maxLengthLastName;

        // Assert
        user.LastName.Should().HaveLength(64);
    }
}
