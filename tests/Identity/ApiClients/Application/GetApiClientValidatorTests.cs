using FluentValidation.TestHelper;

namespace Vitarish.ApiClients.Tests.Application;

/// <summary>
/// Unit tests for the <see cref="GetApiClientValidator"/>.
/// </summary>
public sealed class GetApiClientValidatorTests
{
    private readonly GetApiClientValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_PassesValidation()
    {
        // Arrange
        var query = new GetApiClient(Id: "test-id");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullId_FailsValidation(string? id)
    {
        // Arrange
        var query = new GetApiClient(Id: id!);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(q => q.Id);
    }

    [Theory]
    [InlineData("client-1")]
    [InlineData("CLIENT-2")]
    [InlineData("client_with_underscore")]
    [InlineData("client.with.dots")]
    public void Validate_WithVariousValidIdFormats_PassesValidation(string id)
    {
        // Arrange
        var query = new GetApiClient(Id: id);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
