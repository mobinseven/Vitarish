using FluentAssertions;
using Vitarish.Identity.Core.Users.Application.Queries;

namespace Identity.Core.Tests.Users;

public class GetUserValidatorTests
{
    private readonly GetUserValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsValid()
    {
        // Arrange
        var query = new GetUser(Id: "user-1");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullId_ReturnsInvalid(string? id)
    {
        // Arrange
        var query = new GetUser(Id: id!);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}

public class GetUsersValidatorTests
{
    private readonly GetUsersValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsValid()
    {
        // Arrange
        var query = new GetUsers();

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}

public class GetUserByNameValidatorTests
{
    private readonly GetUserByNameValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsValid()
    {
        // Arrange
        var query = new GetUserByName(UserName: "john.doe");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullUserName_ReturnsInvalid(string? userName)
    {
        // Arrange
        var query = new GetUserByName(UserName: userName!);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserName");
    }
}

public class HasUserAccessValidatorTests
{
    private readonly HasUserAccessValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsValid()
    {
        // Arrange
        var query = new HasUserAccess(UserId: "user-1", ClientId: "client-1");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "client-1")]
    [InlineData(null, "client-1")]
    public void Validate_WithEmptyOrNullUserId_ReturnsInvalid(string? userId, string clientId)
    {
        // Arrange
        var query = new HasUserAccess(UserId: userId!, ClientId: clientId);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Theory]
    [InlineData("user-1", "")]
    [InlineData("user-1", null)]
    public void Validate_WithEmptyOrNullClientId_ReturnsInvalid(string userId, string? clientId)
    {
        // Arrange
        var query = new HasUserAccess(UserId: userId, ClientId: clientId!);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClientId");
    }
}

public class GetUserProfileValidatorTests
{
    private readonly GetUserProfileValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsValid()
    {
        // Arrange
        var query = new GetUserProfile(UserId: "user-1");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullUserId_ReturnsInvalid(string? userId)
    {
        // Arrange
        var query = new GetUserProfile(UserId: userId!);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }
}
