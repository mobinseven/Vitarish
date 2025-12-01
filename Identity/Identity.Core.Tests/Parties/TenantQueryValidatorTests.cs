using FluentAssertions;
using Vitarish.Identity.Core.Parties.Application.Queries;

namespace Identity.Core.Tests.Parties;

public class GetTenantValidatorTests
{
    private readonly GetTenantValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsValid()
    {
        // Arrange
        var query = new GetTenant(Id: Guid.NewGuid());

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyId_ReturnsInvalid()
    {
        // Arrange
        var query = new GetTenant(Id: Guid.Empty);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}

public class GetTenantIndicatorsValidatorTests
{
    private readonly GetTenantIndicatorsValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsValid()
    {
        // Arrange
        var query = new GetTenantIndicators(TenantId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyTenantId_ReturnsInvalid()
    {
        // Arrange
        var query = new GetTenantIndicators(TenantId: Guid.Empty);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }
}

public class GetTenantsByClientIdValidatorTests
{
    private readonly GetTenantsByClientIdValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsValid()
    {
        // Arrange
        var query = new GetTenantsByClientId(ClientId: "client-1");

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullClientId_ReturnsInvalid(string? clientId)
    {
        // Arrange
        var query = new GetTenantsByClientId(ClientId: clientId!);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ClientId");
    }
}

public class GetTenantsByOwnerValidatorTests
{
    private readonly GetTenantsByOwnerValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsValid()
    {
        // Arrange
        var query = new GetTenantsByOwner(OwnerId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyOwnerId_ReturnsInvalid()
    {
        // Arrange
        var query = new GetTenantsByOwner(OwnerId: Guid.Empty);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OwnerId");
    }
}
