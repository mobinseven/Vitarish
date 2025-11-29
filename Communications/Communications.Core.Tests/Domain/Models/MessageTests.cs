namespace Vitarish.Communications.Core.Tests.Domain.Models;

using Vitarish.Communications.Core.Domain.Models;

public class MessageTests
{
    [Fact]
    public void Message_NewInstance_HasNullProperties()
    {
        // Arrange & Act
        var message = new Message();

        // Assert
        Assert.Null(message.Sender);
        Assert.Null(message.Recipient);
        Assert.Null(message.Subject);
        Assert.Null(message.HtmlBody);
        Assert.Null(message.Body);
    }

    [Fact]
    public void Message_SetProperties_ValuesAreCorrect()
    {
        // Arrange & Act
        var message = new Message
        {
            Sender = "sender@example.com",
            Recipient = "recipient@example.com",
            Subject = "Test Subject",
            HtmlBody = "<p>HTML content</p>",
            Body = "Plain text content"
        };

        // Assert
        Assert.Equal("sender@example.com", message.Sender);
        Assert.Equal("recipient@example.com", message.Recipient);
        Assert.Equal("Test Subject", message.Subject);
        Assert.Equal("<p>HTML content</p>", message.HtmlBody);
        Assert.Equal("Plain text content", message.Body);
    }
}

public class SmsSendResultTests
{
    [Fact]
    public void SmsSendResult_Constructor_SetsAllProperties()
    {
        // Arrange
        var packId = Guid.NewGuid();
        var submittedAt = DateTimeOffset.UtcNow;
        var relatedIds = new List<string> { "rel-1", "rel-2" };
        var metadata = new Dictionary<string, string?> { ["key"] = "value" };

        // Act
        var result = new SmsSendResult(
            MessageId: "msg-123",
            Recipient: "+1234567890",
            Cost: 0.05m,
            PackId: packId,
            SubmittedAt: submittedAt,
            RelatedMessageIds: relatedIds,
            Metadata: metadata
        );

        // Assert
        Assert.Equal("msg-123", result.MessageId);
        Assert.Equal("+1234567890", result.Recipient);
        Assert.Equal(0.05m, result.Cost);
        Assert.Equal(packId, result.PackId);
        Assert.Equal(submittedAt, result.SubmittedAt);
        Assert.Equal(2, result.RelatedMessageIds?.Count);
        Assert.Equal("value", result.Metadata?["key"]);
    }

    [Fact]
    public void SmsSendResult_WithNullOptionalValues_WorksCorrectly()
    {
        // Arrange & Act
        var result = new SmsSendResult(
            MessageId: "msg-456",
            Recipient: null,
            Cost: 0m,
            PackId: null,
            SubmittedAt: null,
            RelatedMessageIds: null,
            Metadata: null
        );

        // Assert
        Assert.Equal("msg-456", result.MessageId);
        Assert.Null(result.Recipient);
        Assert.Equal(0m, result.Cost);
        Assert.Null(result.PackId);
        Assert.Null(result.SubmittedAt);
        Assert.Null(result.RelatedMessageIds);
        Assert.Null(result.Metadata);
    }
}

public class SmsDeliveryStateTests
{
    [Fact]
    public void SmsDeliveryState_Constructor_SetsAllProperties()
    {
        // Arrange
        var sentAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var deliveredAt = DateTimeOffset.UtcNow;
        var metadata = new Dictionary<string, string?> { ["provider"] = "test" };

        // Act
        var state = new SmsDeliveryState(
            MessageId: "msg-789",
            Recipient: "+9876543210",
            StatusCode: 200,
            Status: "Delivered",
            MessageText: "Hello World",
            Cost: 0.10m,
            SentAt: sentAt,
            DeliveredAt: deliveredAt,
            Metadata: metadata
        );

        // Assert
        Assert.Equal("msg-789", state.MessageId);
        Assert.Equal("+9876543210", state.Recipient);
        Assert.Equal(200, state.StatusCode);
        Assert.Equal("Delivered", state.Status);
        Assert.Equal("Hello World", state.MessageText);
        Assert.Equal(0.10m, state.Cost);
        Assert.Equal(sentAt, state.SentAt);
        Assert.Equal(deliveredAt, state.DeliveredAt);
        Assert.Equal("test", state.Metadata?["provider"]);
    }

    [Fact]
    public void SmsDeliveryState_WithNullOptionalValues_WorksCorrectly()
    {
        // Arrange & Act
        var state = new SmsDeliveryState(
            MessageId: "msg-000",
            Recipient: null,
            StatusCode: null,
            Status: null,
            MessageText: null,
            Cost: null,
            SentAt: null,
            DeliveredAt: null,
            Metadata: null
        );

        // Assert
        Assert.Equal("msg-000", state.MessageId);
        Assert.Null(state.Recipient);
        Assert.Null(state.StatusCode);
        Assert.Null(state.Status);
        Assert.Null(state.MessageText);
        Assert.Null(state.Cost);
        Assert.Null(state.SentAt);
        Assert.Null(state.DeliveredAt);
        Assert.Null(state.Metadata);
    }
}
