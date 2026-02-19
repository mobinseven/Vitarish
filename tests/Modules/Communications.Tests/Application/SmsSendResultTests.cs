namespace Communications.Tests.Application;

/// <summary>
/// Unit tests for <see cref="SmsSendResult"/> record.
/// </summary>
public sealed class SmsSendResultTests
{
    [Fact]
    public void SmsSendResult_CanBeCreated_WithAllProperties()
    {
        // Arrange
        Guid packId = Guid.NewGuid();
        DateTimeOffset submittedAt = DateTimeOffset.UtcNow;
        string[] relatedIds = ["1", "2", "3"];
        Dictionary<string, string?> metadata = new() { ["key1"] = "value1", ["key2"] = null };

        // Act
        SmsSendResult result = new(
            MessageId: "12345",
            Recipient: "+15551234567",
            Cost: 123.45m,
            PackId: packId,
            SubmittedAt: submittedAt,
            RelatedMessageIds: relatedIds,
            Metadata: metadata);

        // Assert
        Assert.Equal("12345", result.MessageId);
        Assert.Equal("+15551234567", result.Recipient);
        Assert.Equal(123.45m, result.Cost);
        Assert.Equal(packId, result.PackId);
        Assert.Equal(submittedAt, result.SubmittedAt);
        Assert.Equal(relatedIds, result.RelatedMessageIds);
        Assert.Equal(metadata, result.Metadata);
    }

    [Fact]
    public void SmsSendResult_CanBeCreated_WithNullablePropertiesAsNull()
    {
        // Act
        SmsSendResult result = new(
            MessageId: "12345",
            Recipient: null,
            Cost: 0m,
            PackId: null,
            SubmittedAt: null,
            RelatedMessageIds: null,
            Metadata: null);

        // Assert
        Assert.Equal("12345", result.MessageId);
        Assert.Null(result.Recipient);
        Assert.Equal(0m, result.Cost);
        Assert.Null(result.PackId);
        Assert.Null(result.SubmittedAt);
        Assert.Null(result.RelatedMessageIds);
        Assert.Null(result.Metadata);
    }

    [Fact]
    public void SmsSendResult_RecordEquality_Works()
    {
        // Arrange
        Guid packId = Guid.NewGuid();
        SmsSendResult result1 = new("1", "+15551234567", 100m, packId, null, null, null);
        SmsSendResult result2 = new("1", "+15551234567", 100m, packId, null, null, null);
        SmsSendResult result3 = new("2", "+15551234567", 100m, packId, null, null, null);

        // Act & Assert
        Assert.Equal(result1, result2);
        Assert.NotEqual(result1, result3);
    }

    [Fact]
    public void SmsSendResult_WithExpression_CreatesNewRecord()
    {
        // Arrange
        SmsSendResult original = new("1", "+15551234567", 100m, null, null, null, null);

        // Act
        SmsSendResult modified = original with { Cost = 200m };

        // Assert
        Assert.NotSame(original, modified);
        Assert.Equal("1", modified.MessageId);
        Assert.Equal(200m, modified.Cost);
        Assert.Equal(100m, original.Cost);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(999999.99)]
    public void SmsSendResult_AcceptsVariousCostValues(decimal cost)
    {
        // Act
        SmsSendResult result = new("1", "+15551234567", cost, null, null, null, null);

        // Assert
        Assert.Equal(cost, result.Cost);
    }
}
