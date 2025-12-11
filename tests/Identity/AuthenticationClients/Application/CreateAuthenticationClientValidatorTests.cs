using FluentValidation.TestHelper;
using Vitarish.AuthenticationClients.Infrastructure.Validators;

namespace Vitarish.AuthenticationClients.Tests.Application;

/// <summary>
/// Unit tests for the <see cref="CreateAuthenticationClientValidator"/>.
/// </summary>
public sealed class CreateAuthenticationClientValidatorTests
{
    private readonly CreateAuthenticationClientValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        // Arrange
        var command = new CreateAuthenticationClient(
            Id: "test-id",
            Url: "https://app.example.com",
            Title: "Test App",
            Description: "A test application",
            LogoUrl: "https://example.com/logo.png",
            AutoAccess: true);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullId_FailsValidation(string? id)
    {
        // Arrange
        var command = new CreateAuthenticationClient(
            Id: id!,
            Url: "https://app.example.com",
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullUrl_FailsValidation(string? url)
    {
        // Arrange
        var command = new CreateAuthenticationClient(
            Id: "test-id",
            Url: url!,
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Url);
    }

    [Fact]
    public void Validate_WithAllEmptyRequiredFields_FailsAllValidations()
    {
        // Arrange
        var command = new CreateAuthenticationClient(
            Id: "",
            Url: "",
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Id);
        result.ShouldHaveValidationErrorFor(c => c.Url);
    }

    [Fact]
    public void Validate_WithMinimalValidCommand_PassesValidation()
    {
        // Arrange - Only required fields, optional fields null
        var command = new CreateAuthenticationClient(
            Id: "test-id",
            Url: "https://app.example.com",
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: false);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("client-1")]
    [InlineData("CLIENT-2")]
    [InlineData("client_with_underscore")]
    [InlineData("client.with.dots")]
    public void Validate_WithVariousValidIdFormats_PassesValidation(string id)
    {
        // Arrange
        var command = new CreateAuthenticationClient(
            Id: id,
            Url: "https://app.example.com",
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(c => c.Id);
    }

    [Theory]
    [InlineData("https://app.example.com")]
    [InlineData("https://app.example.com:8080")]
    [InlineData("http://localhost:5000")]
    [InlineData("https://app.example.com/callback")]
    public void Validate_WithVariousValidUrlFormats_PassesValidation(string url)
    {
        // Arrange
        var command = new CreateAuthenticationClient(
            Id: "test-id",
            Url: url,
            Title: null,
            Description: null,
            LogoUrl: null,
            AutoAccess: true);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(c => c.Url);
    }
}
