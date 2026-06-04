namespace Communications.Tests.Application;

/// <summary>
/// Unit tests for <see cref="SmsDeliveryState"/> record.
/// </summary>
public sealed class SmsDeliveryStateTests
{
    [Fact]
    public void SmsDeliveryState_CanBeCreated_WithAllProperties()
    {
        // Arrange
        DateTime sentAt = DateTime.UtcNow.AddMinutes(-5);
        DateTime deliveredAt = DateTime.UtcNow;
        Dictionary<string, string?> metadata = new() { ["lineNumber"] = "30004505000017" };

        // Act
        SmsDeliveryState state = new(
            MessageId: "12345",
            Recipient: "+15551234567",
            StatusCode: 2,
            Status: "Delivered",
            MessageText: "Hello World",
            Cost: 50.5m,
            SentAt: sentAt,
            DeliveredAt: deliveredAt,
            Metadata: metadata);

        // Assert
        Assert.Equal("12345", state.MessageId);
        Assert.Equal("+15551234567", state.Recipient);
        Assert.Equal(2, state.StatusCode);
        Assert.Equal("Delivered", state.Status);
        Assert.Equal("Hello World", state.MessageText);
        Assert.Equal(50.5m, state.Cost);
        Assert.Equal(sentAt, state.SentAt);
        Assert.Equal(deliveredAt, state.DeliveredAt);
        Assert.Equal(metadata, state.Metadata);
    }

    [Fact]
    public void SmsDeliveryState_CanBeCreated_WithNullablePropertiesAsNull()
    {
        // Act
        SmsDeliveryState state = new(
            MessageId: "12345",
            Recipient: null,
            StatusCode: null,
            Status: null,
            MessageText: null,
            Cost: null,
            SentAt: null,
            DeliveredAt: null,
            Metadata: null);

        // Assert
        Assert.Equal("12345", state.MessageId);
        Assert.Null(state.Recipient);
        Assert.Null(state.StatusCode);
        Assert.Null(state.Status);
        Assert.Null(state.MessageText);
        Assert.Null(state.Cost);
        Assert.Null(state.SentAt);
        Assert.Null(state.DeliveredAt);
        Assert.Null(state.Metadata);
    }

    [Fact]
    public void SmsDeliveryState_RecordEquality_Works()
    {
        // Arrange
        DateTime time = DateTime.UtcNow;
        SmsDeliveryState state1 = new("1", "+15551234567", 2, "Delivered", "Test", 10m, time, time, null);
        SmsDeliveryState state2 = new("1", "+15551234567", 2, "Delivered", "Test", 10m, time, time, null);
        SmsDeliveryState state3 = new("2", "+15551234567", 2, "Delivered", "Test", 10m, time, time, null);

        // Act & Assert
        Assert.Equal(state1, state2);
        Assert.NotEqual(state1, state3);
    }

    [Fact]
    public void SmsDeliveryState_WithExpression_CreatesNewRecord()
    {
        // Arrange
        SmsDeliveryState original = new("1", "+15551234567", 1, "Pending", "Test", 10m, null, null, null);

        // Act
        SmsDeliveryState modified = original with { StatusCode = 2, Status = "Delivered" };

        // Assert
        Assert.NotSame(original, modified);
        Assert.Equal("1", modified.MessageId);
        Assert.Equal(2, modified.StatusCode);
        Assert.Equal("Delivered", modified.Status);
        Assert.Equal(1, original.StatusCode);
        Assert.Equal("Pending", original.Status);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(999)]
    public void SmsDeliveryState_AcceptsVariousStatusCodes(int statusCode)
    {
        // Act
        SmsDeliveryState state = new("1", null, statusCode, null, null, null, null, null, null);

        // Assert
        Assert.Equal(statusCode, state.StatusCode);
    }
}
