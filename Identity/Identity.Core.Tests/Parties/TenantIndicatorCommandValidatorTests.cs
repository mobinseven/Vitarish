using FluentAssertions;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Core.Parties.Domain.Enums;

namespace Identity.Core.Tests.Parties;

public class AddTenantIndicatorValidatorTests
{
    private readonly AddTenantIndicatorValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new AddTenantIndicator(
            TenantId: Guid.NewGuid(),
            Method: IndicatorMethod.Hostname,
            Value: "app.example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithSubdomainMethod_ReturnsValid()
    {
        // Arrange
        var command = new AddTenantIndicator(
            TenantId: Guid.NewGuid(),
            Method: IndicatorMethod.Subdomain,
            Value: "tenant.myapp.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithIsPrimaryTrue_ReturnsValid()
    {
        // Arrange
        var command = new AddTenantIndicator(
            TenantId: Guid.NewGuid(),
            Method: IndicatorMethod.Hostname,
            Value: "primary.example.com",
            IsPrimary: true);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithManualVerificationMethod_ReturnsValid()
    {
        // Arrange
        var command = new AddTenantIndicator(
            TenantId: Guid.NewGuid(),
            Method: IndicatorMethod.Hostname,
            Value: "manual.example.com",
            VerificationMethod: IndicatorVerificationMethod.Manual);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyTenantId_ReturnsInvalid()
    {
        // Arrange
        var command = new AddTenantIndicator(
            TenantId: Guid.Empty,
            Method: IndicatorMethod.Hostname,
            Value: "app.example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullValue_ReturnsInvalid(string? value)
    {
        // Arrange
        var command = new AddTenantIndicator(
            TenantId: Guid.NewGuid(),
            Method: IndicatorMethod.Hostname,
            Value: value!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Value");
    }

    [Fact]
    public void Validate_WithValueExceedingMaxLength_ReturnsInvalid()
    {
        // Arrange - FQDN max length is 253 characters
        var longValue = new string('a', 254) + ".example.com";
        var command = new AddTenantIndicator(
            TenantId: Guid.NewGuid(),
            Method: IndicatorMethod.Hostname,
            Value: longValue);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Value");
    }

    [Theory]
    [InlineData("not a hostname")]
    [InlineData("invalid..hostname")]
    public void Validate_WithInvalidHostname_ReturnsInvalid(string value)
    {
        // Arrange
        var command = new AddTenantIndicator(
            TenantId: Guid.NewGuid(),
            Method: IndicatorMethod.Hostname,
            Value: value);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Value");
    }
}

public class ActivateTenantIndicatorValidatorTests
{
    private readonly ActivateTenantIndicatorValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new ActivateTenantIndicator(
            TenantId: Guid.NewGuid(),
            IndicatorId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyTenantId_ReturnsInvalid()
    {
        // Arrange
        var command = new ActivateTenantIndicator(
            TenantId: Guid.Empty,
            IndicatorId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Validate_WithEmptyIndicatorId_ReturnsInvalid()
    {
        // Arrange
        var command = new ActivateTenantIndicator(
            TenantId: Guid.NewGuid(),
            IndicatorId: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IndicatorId");
    }

    [Fact]
    public void Validate_WithBothIdsEmpty_ReturnsInvalid()
    {
        // Arrange
        var command = new ActivateTenantIndicator(
            TenantId: Guid.Empty,
            IndicatorId: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
        result.Errors.Should().Contain(e => e.PropertyName == "IndicatorId");
    }
}

public class DisableTenantIndicatorValidatorTests
{
    private readonly DisableTenantIndicatorValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new DisableTenantIndicator(
            TenantId: Guid.NewGuid(),
            IndicatorId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyTenantId_ReturnsInvalid()
    {
        // Arrange
        var command = new DisableTenantIndicator(
            TenantId: Guid.Empty,
            IndicatorId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Validate_WithEmptyIndicatorId_ReturnsInvalid()
    {
        // Arrange
        var command = new DisableTenantIndicator(
            TenantId: Guid.NewGuid(),
            IndicatorId: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IndicatorId");
    }

    [Fact]
    public void Validate_WithBothIdsEmpty_ReturnsInvalid()
    {
        // Arrange
        var command = new DisableTenantIndicator(
            TenantId: Guid.Empty,
            IndicatorId: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
        result.Errors.Should().Contain(e => e.PropertyName == "IndicatorId");
    }
}

public class VerifyTenantIndicatorValidatorTests
{
    private readonly VerifyTenantIndicatorValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new VerifyTenantIndicator(
            TenantId: Guid.NewGuid(),
            IndicatorId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyTenantId_ReturnsInvalid()
    {
        // Arrange
        var command = new VerifyTenantIndicator(
            TenantId: Guid.Empty,
            IndicatorId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Validate_WithEmptyIndicatorId_ReturnsInvalid()
    {
        // Arrange
        var command = new VerifyTenantIndicator(
            TenantId: Guid.NewGuid(),
            IndicatorId: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IndicatorId");
    }

    [Fact]
    public void Validate_WithBothIdsEmpty_ReturnsInvalid()
    {
        // Arrange
        var command = new VerifyTenantIndicator(
            TenantId: Guid.Empty,
            IndicatorId: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
        result.Errors.Should().Contain(e => e.PropertyName == "IndicatorId");
    }
}

public class DeleteTenantIndicatorValidatorTests
{
    private readonly DeleteTenantIndicatorValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new DeleteTenantIndicator(
            TenantId: Guid.NewGuid(),
            IndicatorId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyTenantId_ReturnsInvalid()
    {
        // Arrange
        var command = new DeleteTenantIndicator(
            TenantId: Guid.Empty,
            IndicatorId: Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
    }

    [Fact]
    public void Validate_WithEmptyIndicatorId_ReturnsInvalid()
    {
        // Arrange
        var command = new DeleteTenantIndicator(
            TenantId: Guid.NewGuid(),
            IndicatorId: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IndicatorId");
    }

    [Fact]
    public void Validate_WithBothIdsEmpty_ReturnsInvalid()
    {
        // Arrange
        var command = new DeleteTenantIndicator(
            TenantId: Guid.Empty,
            IndicatorId: Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TenantId");
        result.Errors.Should().Contain(e => e.PropertyName == "IndicatorId");
    }
}
