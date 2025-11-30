using FluentAssertions;
using Vitarish.Identity.Core.Users.Application.Commands;

namespace Identity.Core.Tests.Users;

public class CreateUserWithEmailValidatorTests
{
    private readonly CreateUserWithEmailValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new CreateUserWithEmail(
            Email: "test@example.com",
            FirstName: "John",
            LastName: "Doe");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullEmail_ReturnsInvalid(string? email)
    {
        // Arrange
        var command = new CreateUserWithEmail(
            Email: email!,
            FirstName: "John",
            LastName: "Doe");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    [InlineData("@invalid.com")]
    public void Validate_WithInvalidEmailFormat_ReturnsInvalid(string email)
    {
        // Arrange
        var command = new CreateUserWithEmail(
            Email: email,
            FirstName: "John",
            LastName: "Doe");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}

public class CreateUserWithPhoneValidatorTests
{
    private readonly CreateUserWithPhoneValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new CreateUserWithPhone(
            PhoneNumber: "+1234567890",
            FirstName: "John",
            LastName: "Doe");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullPhoneNumber_ReturnsInvalid(string? phoneNumber)
    {
        // Arrange
        var command = new CreateUserWithPhone(
            PhoneNumber: phoneNumber!,
            FirstName: "John",
            LastName: "Doe");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }
}

public class UpdateUserValidatorTests
{
    private readonly UpdateUserValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new UpdateUser(
            Id: "user-1",
            FirstName: "John",
            LastName: "Doe");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullId_ReturnsInvalid(string? id)
    {
        // Arrange
        var command = new UpdateUser(
            Id: id!,
            FirstName: "John",
            LastName: "Doe");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}

public class DeleteUserValidatorTests
{
    private readonly DeleteUserValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new DeleteUser(Id: "user-1");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullId_ReturnsInvalid(string? id)
    {
        // Arrange
        var command = new DeleteUser(Id: id!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}

public class SetUserAccessClaimValidatorTests
{
    private readonly SetUserAccessClaimValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new SetUserAccessClaim(
            UserId: "user-1",
            ClientId: "client-1",
            AccessValue: "admin");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "client-1")]
    [InlineData(null, "client-1")]
    public void Validate_WithEmptyOrNullUserId_ReturnsInvalid(string? userId, string clientId)
    {
        // Arrange
        var command = new SetUserAccessClaim(
            UserId: userId!,
            ClientId: clientId,
            AccessValue: "admin");

        // Act
        var result = _validator.Validate(command);

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
        var command = new SetUserAccessClaim(
            UserId: userId,
            ClientId: clientId!,
            AccessValue: "admin");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClientId");
    }
}

public class RemoveUserAccessClaimValidatorTests
{
    private readonly RemoveUserAccessClaimValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new RemoveUserAccessClaim(
            UserId: "user-1",
            ClientId: "client-1");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "client-1")]
    [InlineData(null, "client-1")]
    public void Validate_WithEmptyOrNullUserId_ReturnsInvalid(string? userId, string clientId)
    {
        // Arrange
        var command = new RemoveUserAccessClaim(
            UserId: userId!,
            ClientId: clientId);

        // Act
        var result = _validator.Validate(command);

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
        var command = new RemoveUserAccessClaim(
            UserId: userId,
            ClientId: clientId!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClientId");
    }
}

public class UpdateUserProfileValidatorTests
{
    private readonly UpdateUserProfileValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new UpdateUserProfile(
            UserId: "user-1",
            Preferences: new Dictionary<string, string> { ["theme"] = "dark" });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullUserId_ReturnsInvalid(string? userId)
    {
        // Arrange
        var command = new UpdateUserProfile(
            UserId: userId!,
            Preferences: new Dictionary<string, string> { ["theme"] = "dark" });

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Fact]
    public void Validate_WithNullPreferences_ReturnsInvalid()
    {
        // Arrange
        var command = new UpdateUserProfile(
            UserId: "user-1",
            Preferences: null!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Preferences");
    }
}

public class SetUserEmailValidatorTests
{
    private readonly SetUserEmailValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new SetUserEmail(
            UserId: "user-1",
            Email: "test@example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullUserId_ReturnsInvalid(string? userId)
    {
        // Arrange
        var command = new SetUserEmail(
            UserId: userId!,
            Email: "test@example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullEmail_ReturnsInvalid(string? email)
    {
        // Arrange
        var command = new SetUserEmail(
            UserId: "user-1",
            Email: email!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("invalid@")]
    public void Validate_WithInvalidEmailFormat_ReturnsInvalid(string email)
    {
        // Arrange
        var command = new SetUserEmail(
            UserId: "user-1",
            Email: email);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}

public class SetUserPhoneNumberValidatorTests
{
    private readonly SetUserPhoneNumberValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new SetUserPhoneNumber(
            UserId: "user-1",
            PhoneNumber: "+1234567890");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullUserId_ReturnsInvalid(string? userId)
    {
        // Arrange
        var command = new SetUserPhoneNumber(
            UserId: userId!,
            PhoneNumber: "+1234567890");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullPhoneNumber_ReturnsInvalid(string? phoneNumber)
    {
        // Arrange
        var command = new SetUserPhoneNumber(
            UserId: "user-1",
            PhoneNumber: phoneNumber!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }
}
