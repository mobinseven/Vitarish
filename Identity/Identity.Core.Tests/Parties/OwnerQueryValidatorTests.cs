using FluentAssertions;
using Vitarish.Identity.Core.Parties.Application.Queries;

namespace Identity.Core.Tests.Parties;

public class GetOwnerValidatorTests
{
    private readonly GetOwnerValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsValid()
    {
        // Arrange
        var query = new GetOwner(Id: Guid.NewGuid());

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyId_ReturnsInvalid()
    {
        // Arrange
        var query = new GetOwner(Id: Guid.Empty);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}
