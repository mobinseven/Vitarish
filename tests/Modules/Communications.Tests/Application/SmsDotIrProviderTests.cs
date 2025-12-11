using Vitarish.Communications.Infrastructure.Wrappers.SmsDotIr;

namespace Vitarish.Communications.Tests.Application;

/// <summary>
/// Unit tests for <see cref="SmsDotIrProvider"/>.
/// </summary>
public sealed class SmsDotIrProviderTests
{
    private static IOptions<ProvidersOptions> CreateOptions(
        string key = "test-api-key",
        string lineNumber = "30004505000017",
        string templateId = "template-123")
    {
        var Providers = new ProvidersOptions
        {
            SmsDotIr = new SmsDotIrOptions
            {
                Key = key,
                LineNumber = lineNumber,
                VerificationCodeTemplateId = templateId
            }
        };
        Mock<IOptions<ProvidersOptions>> mock = new();
        mock.Setup(x => x.Value).Returns(Providers);
        return mock.Object;
    }

    [Fact]
    public void Constructor_WithValidOptions_CreatesInstance()
    {
        // Arrange
        IOptions<ProvidersOptions> options = CreateOptions();

        // Act
        SmsDotIrProvider provider = new(options);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task SendSms_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        IOptions<ProvidersOptions> options = CreateOptions();
        SmsDotIrProvider provider = new(options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => provider.SendSms(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendSms_WithInvalidRecipient_ThrowsArgumentException(string? recipient)
    {
        // Arrange
        IOptions<ProvidersOptions> options = CreateOptions();
        SmsDotIrProvider provider = new(options);
        Message message = new() { Recipient = recipient, Body = "Hello" };

        // Act & Assert
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.SendSms(message));
        Assert.Contains("Recipient is required", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendSms_WithInvalidBody_ThrowsArgumentException(string? body)
    {
        // Arrange
        IOptions<ProvidersOptions> options = CreateOptions();
        SmsDotIrProvider provider = new(options);
        Message message = new() { Recipient = "+15551234567", Body = body };

        // Act & Assert
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.SendSms(message));
        Assert.Contains("Body is required", exception.Message);
    }

    [Fact]
    public async Task SendCode_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        IOptions<ProvidersOptions> options = CreateOptions();
        SmsDotIrProvider provider = new(options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => provider.SendCode(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendCode_WithInvalidRecipient_ThrowsArgumentException(string? recipient)
    {
        // Arrange
        IOptions<ProvidersOptions> options = CreateOptions();
        SmsDotIrProvider provider = new(options);
        Message message = new() { Recipient = recipient, Body = "123456" };

        // Act & Assert
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.SendCode(message));
        Assert.Contains("Recipient is required", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SendCode_WithInvalidBody_ThrowsArgumentException(string? body)
    {
        // Arrange
        IOptions<ProvidersOptions> options = CreateOptions();
        SmsDotIrProvider provider = new(options);
        Message message = new() { Recipient = "+15551234567", Body = body };

        // Act & Assert
        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            () => provider.SendCode(message));
        Assert.Contains("Code is required", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(long.MinValue)]
    public async Task GetSmsDeliveryStateAsync_WithNonPositiveMessageId_ThrowsArgumentOutOfRangeException(long messageId)
    {
        // Arrange
        IOptions<ProvidersOptions> options = CreateOptions();
        SmsDotIrProvider provider = new(options);

        // Act & Assert
        ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => provider.GetSmsDeliveryStateAsync(messageId));
        Assert.Contains("Message ID must be a positive value", exception.Message);
    }
}
