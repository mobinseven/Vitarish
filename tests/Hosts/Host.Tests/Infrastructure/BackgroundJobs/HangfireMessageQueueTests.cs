using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Vitarish.Host.Infrastructure.BackgroundJobs;

namespace Host.Tests.Infrastructure.BackgroundJobs;

public class HangfireMessageQueueTests
{
    private readonly Mock<IBackgroundJobClient> _mockBackgroundJobClient;
    private readonly HangfireMessageQueue _sut;

    public HangfireMessageQueueTests()
    {
        _mockBackgroundJobClient = new Mock<IBackgroundJobClient>();
        _sut = new HangfireMessageQueue(_mockBackgroundJobClient.Object);
    }

    [Fact]
    public void EnqueueEmail_WithValidParameters_EnqueuesJob()
    {
        // Arrange
        string to = "test@example.com";
        string subject = "Test Subject";
        string textBody = "Test Text Body";
        string htmlBody = "<p>Test HTML Body</p>";

        // Act
        _sut.EnqueueEmail(to, subject, textBody, htmlBody);

        // Assert
        _mockBackgroundJobClient.Verify(
            x => x.Create(
                It.Is<Job>(job =>
                    job.Type == typeof(MessageProcessor) &&
                    job.Method.Name == nameof(MessageProcessor.ProcessMessage) &&
                    job.Args[0] is EmailMessage),
                It.IsAny<EnqueuedState>()),
            Times.Once);
    }

    [Fact]
    public void EnqueueEmail_WithNullTo_ThrowsArgumentNullException()
    {
        // Arrange
        string? to = null;
        string subject = "Test Subject";
        string textBody = "Test Text Body";
        string htmlBody = "<p>Test HTML Body</p>";

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            _sut.EnqueueEmail(to!, subject, textBody, htmlBody));
    }

    [Fact]
    public void EnqueueEmail_WithEmptyTo_ThrowsArgumentException()
    {
        // Arrange
        string to = "";
        string subject = "Test Subject";
        string textBody = "Test Text Body";
        string htmlBody = "<p>Test HTML Body</p>";

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() =>
            _sut.EnqueueEmail(to, subject, textBody, htmlBody));
    }

    [Fact]
    public void EnqueueEmail_WithNullSubject_ThrowsArgumentNullException()
    {
        // Arrange
        string to = "test@example.com";
        string? subject = null;
        string textBody = "Test Text Body";
        string htmlBody = "<p>Test HTML Body</p>";

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            _sut.EnqueueEmail(to, subject!, textBody, htmlBody));
    }

    [Fact]
    public void EnqueueSms_WithValidParameters_EnqueuesJob()
    {
        // Arrange
        string phoneNumber = "+1234567890";
        string code = "123456";

        // Act
        _sut.EnqueueSms(phoneNumber, code);

        // Assert
        _mockBackgroundJobClient.Verify(
            x => x.Create(
                It.Is<Job>(job =>
                    job.Type == typeof(MessageProcessor) &&
                    job.Method.Name == nameof(MessageProcessor.ProcessMessage) &&
                    job.Args[0] is SmsMessage),
                It.IsAny<EnqueuedState>()),
            Times.Once);
    }

    [Fact]
    public void EnqueueSms_WithNullPhoneNumber_ThrowsArgumentNullException()
    {
        // Arrange
        string? phoneNumber = null;
        string code = "123456";

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            _sut.EnqueueSms(phoneNumber!, code));
    }

    [Fact]
    public void EnqueueSms_WithEmptyPhoneNumber_ThrowsArgumentException()
    {
        // Arrange
        string phoneNumber = "";
        string code = "123456";

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() =>
            _sut.EnqueueSms(phoneNumber, code));
    }

    [Fact]
    public void EnqueueSms_WithNullCode_ThrowsArgumentNullException()
    {
        // Arrange
        string phoneNumber = "+1234567890";
        string? code = null;

        // Act & Assert
        _ = Assert.Throws<ArgumentNullException>(() =>
            _sut.EnqueueSms(phoneNumber, code!));
    }

    [Fact]
    public void EnqueueSms_WithEmptyCode_ThrowsArgumentException()
    {
        // Arrange
        string phoneNumber = "+1234567890";
        string code = "";

        // Act & Assert
        _ = Assert.Throws<ArgumentException>(() =>
            _sut.EnqueueSms(phoneNumber, code));
    }
}
