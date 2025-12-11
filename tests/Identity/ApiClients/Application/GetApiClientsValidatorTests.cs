using FluentValidation.TestHelper;

namespace Vitarish.ApiClients.Tests.Application;

/// <summary>
/// Unit tests for the <see cref="GetApiClientsValidator"/>.
/// </summary>
public sealed class GetApiClientsValidatorTests
{
    private readonly GetApiClientsValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_PassesValidation()
    {
        // Arrange
        var query = new GetApiClients();

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_HasNoRules_AlwaysPasses()
    {
        // The GetApiClients query has no parameters, so validation always passes
        // This test documents that behavior

        // Arrange
        var query = new GetApiClients();

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
