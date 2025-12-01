using FluentAssertions;
using Vitarish.Communications.Core.Messages.Application.Queries;

namespace Communications.Core.Tests.Messages;

public class GetSmsDeliveryStateValidatorTests
{
    private readonly GetSmsDeliveryStateValidator _validator = new();

    [Fact]
    public void Validate_WithValidQuery_ReturnsValid()
    {
        // Arrange
        var query = new GetSmsDeliveryState(MessageId: 12345);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validate_WithZeroOrNegativeMessageId_ReturnsInvalid(long messageId)
    {
        // Arrange
        var query = new GetSmsDeliveryState(MessageId: messageId);

        // Act
        var result = _validator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MessageId");
    }
}
