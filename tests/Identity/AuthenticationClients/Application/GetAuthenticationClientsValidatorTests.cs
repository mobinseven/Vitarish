using FluentValidation.TestHelper;

namespace Vitarish.AuthenticationClients.Tests.Application;

/// <summary>
/// Unit tests for the <see cref="GetAuthenticationClientsValidator"/>.
/// </summary>
public sealed class GetAuthenticationClientsValidatorTests
{
    private readonly GetAuthenticationClientsValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_PassesValidation()
    {
        // Arrange
        var query = new GetAuthenticationClients();

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_HasNoRules_AlwaysPasses()
    {
        // Arrange
        var query = new GetAuthenticationClients();

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
