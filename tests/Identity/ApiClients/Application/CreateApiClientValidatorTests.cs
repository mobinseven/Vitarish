using FluentValidation.TestHelper;
using Vitarish.ApiClients.Infrastructure.Validators;

namespace Vitarish.ApiClients.Tests.Application;

/// <summary>
/// Unit tests for the <see cref="CreateApiClientValidator"/>.
/// </summary>
public sealed class CreateApiClientValidatorTests
{
    private readonly CreateApiClientValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        // Arrange
        var command = new CreateApiClient(
            Id: "test-id",
            Secret: "test-secret",
            Scopes: "read write",
            HostUri: "https://api.example.com");

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
        var command = new CreateApiClient(
            Id: id!,
            Secret: "test-secret",
            Scopes: "read write",
            HostUri: "https://api.example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullSecret_FailsValidation(string? secret)
    {
        // Arrange
        var command = new CreateApiClient(
            Id: "test-id",
            Secret: secret!,
            Scopes: "read write",
            HostUri: "https://api.example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Secret);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullScopes_FailsValidation(string? scopes)
    {
        // Arrange
        var command = new CreateApiClient(
            Id: "test-id",
            Secret: "test-secret",
            Scopes: scopes!,
            HostUri: "https://api.example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Scopes);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullHostUri_FailsValidation(string? hostUri)
    {
        // Arrange
        var command = new CreateApiClient(
            Id: "test-id",
            Secret: "test-secret",
            Scopes: "read write",
            HostUri: hostUri!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.HostUri);
    }

    [Fact]
    public void Validate_WithAllEmptyFields_FailsAllValidations()
    {
        // Arrange
        var command = new CreateApiClient(
            Id: "",
            Secret: "",
            Scopes: "",
            HostUri: "");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Id);
        result.ShouldHaveValidationErrorFor(c => c.Secret);
        result.ShouldHaveValidationErrorFor(c => c.Scopes);
        result.ShouldHaveValidationErrorFor(c => c.HostUri);
    }
}
