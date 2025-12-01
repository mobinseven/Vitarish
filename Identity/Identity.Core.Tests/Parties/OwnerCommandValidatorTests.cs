using FluentAssertions;
using Vitarish.Identity.Core.Parties.Application.Commands;

namespace Identity.Core.Tests.Parties;

public class CreateOwnerValidatorTests
{
    private readonly CreateOwnerValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new CreateOwner(Name: "Test Owner");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithAllOptionalFields_ReturnsValid()
    {
        // Arrange
        var command = new CreateOwner(
            Name: "Test Owner",
            LogoUrl: "https://example.com/logo.png",
            Description: "Test Description",
            CreatedBy: "admin");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullName_ReturnsInvalid(string? name)
    {
        // Arrange
        var command = new CreateOwner(Name: name!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}

public class UpdateOwnerValidatorTests
{
    private readonly UpdateOwnerValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new UpdateOwner(
            Id: Guid.NewGuid(),
            Name: "Updated Owner");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithAllOptionalFields_ReturnsValid()
    {
        // Arrange
        var command = new UpdateOwner(
            Id: Guid.NewGuid(),
            Name: "Updated Owner",
            LogoUrl: "https://example.com/logo.png",
            Description: "Updated Description",
            ModifiedBy: "admin");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyId_ReturnsInvalid()
    {
        // Arrange
        var command = new UpdateOwner(
            Id: Guid.Empty,
            Name: "Updated Owner");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullName_ReturnsInvalid(string? name)
    {
        // Arrange
        var command = new UpdateOwner(
            Id: Guid.NewGuid(),
            Name: name!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithBothIdAndNameInvalid_ReturnsMultipleErrors()
    {
        // Arrange
        var command = new UpdateOwner(
            Id: Guid.Empty,
            Name: "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }
}

public class DeleteOwnerValidatorTests
{
    private readonly DeleteOwnerValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new DeleteOwner(Id: Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyId_ReturnsInvalid()
    {
        // Arrange
        var command = new DeleteOwner(Id: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
