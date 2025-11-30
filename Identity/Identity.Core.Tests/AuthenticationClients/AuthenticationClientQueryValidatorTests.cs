using FluentAssertions;
using Vitarish.Identity.Core.AuthenticationClients.Application.Queries;

namespace Identity.Core.Tests.AuthenticationClients;

public class GetAuthenticationClientValidatorTests
{
    private readonly GetAuthenticationClientValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsValid()
    {
        // Arrange
        var query = new GetAuthenticationClient(Id: "client-1");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullId_ReturnsInvalid(string? id)
    {
        // Arrange
        var query = new GetAuthenticationClient(Id: id!);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}

public class GetAuthenticationClientsValidatorTests
{
    private readonly GetAuthenticationClientsValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsValid()
    {
        // Arrange
        var query = new GetAuthenticationClients();

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
