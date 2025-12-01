using FluentAssertions;
using Vitarish.Identity.Core.Users.Domain.Entities;

namespace Vitarish.Identity.Core.Tests.Users;

public class UserEntityTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesUser()
    {
        // Arrange & Act
        var user = new User(
            id: "user-1",
            userName: "john.doe",
            email: "john@example.com",
            phoneNumber: "+1234567890",
            firstName: "John",
            lastName: "Doe");

        // Assert
        user.Id.Should().Be("user-1");
        user.UserName.Should().Be("john.doe");
        user.Email.Should().Be("john@example.com");
        user.PhoneNumber.Should().Be("+1234567890");
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
    }

    [Fact]
    public void Constructor_WithMinimalParameters_CreatesUser()
    {
        // Arrange & Act
        var user = new User(
            id: "user-1",
            userName: "john.doe");

        // Assert
        user.Id.Should().Be("user-1");
        user.UserName.Should().Be("john.doe");
        user.Email.Should().BeNull();
        user.PhoneNumber.Should().BeNull();
        user.FirstName.Should().BeNull();
        user.LastName.Should().BeNull();
    }

    [Fact]
    public void UpdateName_WithValidParameters_UpdatesNameFields()
    {
        // Arrange
        var user = new User(
            id: "user-1",
            userName: "john.doe",
            firstName: "John",
            lastName: "Doe");

        // Act
        user.UpdateName("Jane", "Smith");

        // Assert
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
    }

    [Fact]
    public void UpdateName_WithNullParameters_SetsNullValues()
    {
        // Arrange
        var user = new User(
            id: "user-1",
            userName: "john.doe",
            firstName: "John",
            lastName: "Doe");

        // Act
        user.UpdateName(null, null);

        // Assert
        user.FirstName.Should().BeNull();
        user.LastName.Should().BeNull();
    }

    [Fact]
    public void UpdateEmail_WithValidEmail_UpdatesEmail()
    {
        // Arrange
        var user = new User(
            id: "user-1",
            userName: "john.doe",
            email: "old@example.com");

        // Act
        user.UpdateEmail("new@example.com");

        // Assert
        user.Email.Should().Be("new@example.com");
    }

    [Fact]
    public void UpdateEmail_WithNull_SetsEmailToNull()
    {
        // Arrange
        var user = new User(
            id: "user-1",
            userName: "john.doe",
            email: "old@example.com");

        // Act
        user.UpdateEmail(null);

        // Assert
        user.Email.Should().BeNull();
    }

    [Fact]
    public void UpdatePhoneNumber_WithValidPhoneNumber_UpdatesPhoneNumber()
    {
        // Arrange
        var user = new User(
            id: "user-1",
            userName: "john.doe",
            phoneNumber: "+1111111111");

        // Act
        user.UpdatePhoneNumber("+2222222222");

        // Assert
        user.PhoneNumber.Should().Be("+2222222222");
    }

    [Fact]
    public void UpdatePhoneNumber_WithNull_SetsPhoneNumberToNull()
    {
        // Arrange
        var user = new User(
            id: "user-1",
            userName: "john.doe",
            phoneNumber: "+1111111111");

        // Act
        user.UpdatePhoneNumber(null);

        // Assert
        user.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public void UpdateProfilePreferences_WithValidPreferences_UpdatesProfilePreferences()
    {
        // Arrange
        var user = new User(
            id: "user-1",
            userName: "john.doe");
        var preferences = new Dictionary<string, string>
        {
            ["theme"] = "dark",
            ["language"] = "en"
        };

        // Act
        user.UpdateProfilePreferences(preferences);

        // Assert
        user.ProfilePreferences.Should().BeEquivalentTo(preferences);
    }

    [Fact]
    public void UpdateProfilePreferences_WithEmptyDictionary_ClearsPreferences()
    {
        // Arrange
        var user = new User(
            id: "user-1",
            userName: "john.doe");
        user.UpdateProfilePreferences(new Dictionary<string, string> { ["theme"] = "dark" });

        // Act
        user.UpdateProfilePreferences(new Dictionary<string, string>());

        // Assert
        user.ProfilePreferences.Should().BeEmpty();
        user.ProfileJson.Should().BeNull();
    }

    [Fact]
    public void ProfilePreferences_WhenProfileJsonIsNull_ReturnsEmptyDictionary()
    {
        // Arrange
        var user = new User(
            id: "user-1",
            userName: "john.doe");

        // Act & Assert
        user.ProfilePreferences.Should().BeEmpty();
    }

    [Fact]
    public void ProfilePreferences_AfterMultipleAccesses_ReturnsCachedValue()
    {
        // Arrange
        var user = new User(
            id: "user-1",
            userName: "john.doe");
        var preferences = new Dictionary<string, string> { ["theme"] = "dark" };
        user.UpdateProfilePreferences(preferences);

        // Act
        var firstAccess = user.ProfilePreferences;
        var secondAccess = user.ProfilePreferences;

        // Assert
        firstAccess.Should().BeEquivalentTo(preferences);
        secondAccess.Should().BeSameAs(firstAccess);
    }
}
