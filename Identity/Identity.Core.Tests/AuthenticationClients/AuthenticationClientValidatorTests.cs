using FluentAssertions;
using Vitarish.Identity.Core.AuthenticationClients.Application.Commands;

namespace Identity.Core.Tests.AuthenticationClients;

public class CreateAuthenticationClientValidatorTests
{
    private readonly CreateAuthenticationClientValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new CreateAuthenticationClient(
            Id: "client-1",
            Url: "https://example.com",
            Title: "Test Client",
            Description: "Test Description",
            LogoUrl: "https://example.com/logo.png",
            AutoAccess: true);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "https://example.com")]
    [InlineData(null, "https://example.com")]
    public void Validate_WithEmptyOrNullId_ReturnsInvalid(string? id, string url)
    {
        // Arrange
        var command = new CreateAuthenticationClient(
            Id: id!,
            Url: url,
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Theory]
    [InlineData("client-1", "")]
    [InlineData("client-1", null)]
    public void Validate_WithEmptyOrNullUrl_ReturnsInvalid(string id, string? url)
    {
        // Arrange
        var command = new CreateAuthenticationClient(
            Id: id,
            Url: url!,
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }
}

public class UpdateAuthenticationClientValidatorTests
{
    private readonly UpdateAuthenticationClientValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new UpdateAuthenticationClient(
            Id: "client-1",
            Url: "https://example.com",
            Title: "Test Client",
            Description: "Test Description",
            LogoUrl: "https://example.com/logo.png",
            AutoAccess: true);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "https://example.com")]
    [InlineData(null, "https://example.com")]
    public void Validate_WithEmptyOrNullId_ReturnsInvalid(string? id, string url)
    {
        // Arrange
        var command = new UpdateAuthenticationClient(
            Id: id!,
            Url: url,
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Theory]
    [InlineData("client-1", "")]
    [InlineData("client-1", null)]
    public void Validate_WithEmptyOrNullUrl_ReturnsInvalid(string id, string? url)
    {
        // Arrange
        var command = new UpdateAuthenticationClient(
            Id: id,
            Url: url!,
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url");
    }
}

public class DeleteAuthenticationClientValidatorTests
{
    private readonly DeleteAuthenticationClientValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new DeleteAuthenticationClient(Id: "client-1");

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
        var command = new DeleteAuthenticationClient(Id: id!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
