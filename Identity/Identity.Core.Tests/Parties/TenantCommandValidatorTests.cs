using FluentAssertions;
using Vitarish.Identity.Core.Parties.Application.Commands;

namespace Identity.Core.Tests.Parties;

public class CreateTenantValidatorTests
{
    private readonly CreateTenantValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new CreateTenant(
            OwnerId: Guid.NewGuid(),
            Name: "Test Tenant",
            ClientId: "client-1");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithAllOptionalFields_ReturnsValid()
    {
        // Arrange
        var command = new CreateTenant(
            OwnerId: Guid.NewGuid(),
            Name: "Test Tenant",
            ClientId: "client-1",
            LogoUrl: "https://example.com/logo.png",
            Description: "Test Description",
            CreatedBy: "admin");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyOwnerId_ReturnsInvalid()
    {
        // Arrange
        var command = new CreateTenant(
            OwnerId: Guid.Empty,
            Name: "Test Tenant",
            ClientId: "client-1");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OwnerId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullName_ReturnsInvalid(string? name)
    {
        // Arrange
        var command = new CreateTenant(
            OwnerId: Guid.NewGuid(),
            Name: name!,
            ClientId: "client-1");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullClientId_ReturnsInvalid(string? clientId)
    {
        // Arrange
        var command = new CreateTenant(
            OwnerId: Guid.NewGuid(),
            Name: "Test Tenant",
            ClientId: clientId!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClientId");
    }
}

public class UpdateTenantValidatorTests
{
    private readonly UpdateTenantValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new UpdateTenant(
            Id: Guid.NewGuid(),
            Name: "Updated Tenant",
            ClientId: "client-1");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithAllOptionalFields_ReturnsValid()
    {
        // Arrange
        var command = new UpdateTenant(
            Id: Guid.NewGuid(),
            Name: "Updated Tenant",
            ClientId: "client-1",
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
        var command = new UpdateTenant(
            Id: Guid.Empty,
            Name: "Updated Tenant",
            ClientId: "client-1");

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
        var command = new UpdateTenant(
            Id: Guid.NewGuid(),
            Name: name!,
            ClientId: "client-1");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullClientId_ReturnsInvalid(string? clientId)
    {
        // Arrange
        var command = new UpdateTenant(
            Id: Guid.NewGuid(),
            Name: "Updated Tenant",
            ClientId: clientId!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClientId");
    }
}

public class DeleteTenantValidatorTests
{
    private readonly DeleteTenantValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new DeleteTenant(Id: Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyId_ReturnsInvalid()
    {
        // Arrange
        var command = new DeleteTenant(Id: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
