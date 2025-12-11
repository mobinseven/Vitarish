using FluentValidation.TestHelper;

namespace Vitarish.ApiClients.Tests.Application;

/// <summary>
/// Unit tests for the <see cref="DeleteApiClientValidator"/>.
/// </summary>
public sealed class DeleteApiClientValidatorTests
{
    private readonly DeleteApiClientValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        // Arrange
        var command = new DeleteApiClient(Id: "test-id");

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
        var command = new DeleteApiClient(Id: id!);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Theory]
    [InlineData("client-1")]
    [InlineData("CLIENT-2")]
    [InlineData("client_with_underscore")]
    [InlineData("client.with.dots")]
    public void Validate_WithVariousValidIdFormats_PassesValidation(string id)
    {
        // Arrange
        var command = new DeleteApiClient(Id: id);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
