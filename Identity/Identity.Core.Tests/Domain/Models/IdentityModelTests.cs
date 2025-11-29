namespace Vitarish.Identity.Core.Tests.Domain.Models;

using Vitarish.Identity.Core.Domain.Models;

public class ClaimDtoTests
{
    [Fact]
    public void ClaimDto_DefaultConstructor_HasNullProperties()
    {
        // Arrange & Act
        var claim = new ClaimDto();

        // Assert
        Assert.Null(claim.Type);
        Assert.Null(claim.Value);
    }

    [Fact]
    public void ClaimDto_ParameterizedConstructor_SetsProperties()
    {
        // Arrange & Act
        var claim = new ClaimDto("role", "admin");

        // Assert
        Assert.Equal("role", claim.Type);
        Assert.Equal("admin", claim.Value);
    }

    [Fact]
    public void ClaimDto_ToClaim_CreatesClaim()
    {
        // Arrange
        var dto = new ClaimDto("permission", "read");

        // Act
        var claim = dto.ToClaim();

        // Assert
        Assert.Equal("permission", claim.Type);
        Assert.Equal("read", claim.Value);
    }

    [Fact]
    public void ClaimDto_ToClaim_WithNullValues_UsesEmptyStrings()
    {
        // Arrange
        var dto = new ClaimDto();

        // Act
        var claim = dto.ToClaim();

        // Assert
        Assert.Equal(string.Empty, claim.Type);
        Assert.Equal(string.Empty, claim.Value);
    }
}

public class UserDtoTests
{
    [Fact]
    public void UserDto_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var dto = new UserDto();

        // Assert
        Assert.Equal(string.Empty, dto.Id);
        Assert.Null(dto.UserName);
        Assert.Null(dto.FirstName);
        Assert.Null(dto.LastName);
        Assert.Null(dto.Email);
        Assert.Null(dto.PhoneNumber);
        Assert.Null(dto.AccessClaim);
        Assert.Null(dto.ProfilePreferences);
    }

    [Fact]
    public void UserDto_FullName_CombinesFirstAndLastName()
    {
        // Arrange
        var dto = new UserDto
        {
            FirstName = "Alice",
            LastName = "Johnson"
        };

        // Assert
        Assert.Equal("Alice Johnson", dto.FullName);
    }

    [Fact]
    public void UserDto_FullName_OnlyFirstName_ReturnsTrimmed()
    {
        // Arrange
        var dto = new UserDto { FirstName = "Bob" };

        // Assert
        Assert.Equal("Bob", dto.FullName);
    }

    [Fact]
    public void UserDto_FullName_OnlyLastName_ReturnsTrimmed()
    {
        // Arrange
        var dto = new UserDto { LastName = "Wilson" };

        // Assert
        Assert.Equal("Wilson", dto.FullName);
    }

    [Fact]
    public void UserDto_FullName_NoNames_ReturnsEmpty()
    {
        // Arrange
        var dto = new UserDto();

        // Assert
        Assert.Equal("", dto.FullName);
    }

    [Fact]
    public void UserDto_SetAllProperties_ValuesAreCorrect()
    {
        // Arrange
        var accessClaim = new ClaimDto("access", "level1");
        var preferences = new Dictionary<string, string> { ["pref"] = "val" };

        // Act
        var dto = new UserDto
        {
            Id = "user-123",
            UserName = "johndoe",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PhoneNumber = "+1111111111",
            AccessClaim = accessClaim,
            ProfilePreferences = preferences
        };

        // Assert
        Assert.Equal("user-123", dto.Id);
        Assert.Equal("johndoe", dto.UserName);
        Assert.Equal("John", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
        Assert.Equal("john@example.com", dto.Email);
        Assert.Equal("+1111111111", dto.PhoneNumber);
        Assert.Equal(accessClaim, dto.AccessClaim);
        Assert.Equal(preferences, dto.ProfilePreferences);
    }
}
