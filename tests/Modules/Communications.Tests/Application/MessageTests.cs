namespace Communications.Tests.Application;

/// <summary>
/// Unit tests for <see cref="Message"/> model.
/// </summary>
public sealed class MessageTests
{
    [Fact]
    public void Message_CanBeCreated_WithDefaultValues()
    {
        // Arrange & Act
        Message message = new();

        // Assert
        Assert.Null(message.Sender);
        Assert.Null(message.Recipient);
        Assert.Null(message.Subject);
        Assert.Null(message.HtmlBody);
        Assert.Null(message.Body);
    }

    [Fact]
    public void Message_CanSetAllProperties()
    {
        // Arrange & Act
        Message message = new()
        {
            Sender = "sender@example.com",
            Recipient = "+15551234567",
            Subject = "Test Subject",
            HtmlBody = "<p>Hello</p>",
            Body = "Hello"
        };

        // Assert
        Assert.Equal("sender@example.com", message.Sender);
        Assert.Equal("+15551234567", message.Recipient);
        Assert.Equal("Test Subject", message.Subject);
        Assert.Equal("<p>Hello</p>", message.HtmlBody);
        Assert.Equal("Hello", message.Body);
    }

    [Theory]
    [InlineData("", "+1234567890", "Subject", "<p>Hi</p>", "Hi")]
    [InlineData(null, "+1234567890", "Subject", "<p>Hi</p>", "Hi")]
    [InlineData("test@test.com", "", null, null, null)]
    public void Message_AcceptsVariousPropertyCombinations(
        string? sender,
        string? recipient,
        string? subject,
        string? htmlBody,
        string? body)
    {
        // Arrange & Act
        Message message = new()
        {
            Sender = sender,
            Recipient = recipient,
            Subject = subject,
            HtmlBody = htmlBody,
            Body = body
        };

        // Assert
        Assert.Equal(sender, message.Sender);
        Assert.Equal(recipient, message.Recipient);
        Assert.Equal(subject, message.Subject);
        Assert.Equal(htmlBody, message.HtmlBody);
        Assert.Equal(body, message.Body);
    }

    [Fact]
    public void Message_PropertiesAreMutable()
    {
        // Arrange
        Message message = new() { Sender = "initial@test.com" };

        // Act
        message.Sender = "updated@test.com";
        message.Recipient = "+15559876543";

        // Assert
        Assert.Equal("updated@test.com", message.Sender);
        Assert.Equal("+15559876543", message.Recipient);
    }
}
