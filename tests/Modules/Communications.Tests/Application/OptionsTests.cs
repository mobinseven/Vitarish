namespace Communications.Tests.Application;

/// <summary>
/// Unit tests for options classes in the Communications domain.
/// </summary>
public sealed class OptionsTests
{
    #region SmsDotIrOptions Tests

    [Fact]
    public void SmsDotIrOptions_HasDefaultValues()
    {
        // Arrange & Act
        SmsDotIrOptions options = new();

        // Assert
        Assert.Equal(string.Empty, options.Key);
        Assert.Equal(string.Empty, options.LineNumber);
        Assert.Equal(string.Empty, options.VerificationCodeTemplateId);
    }

    [Fact]
    public void SmsDotIrOptions_CanSetAllProperties()
    {
        // Arrange & Act
        SmsDotIrOptions options = new()
        {
            Key = "api-key-123",
            LineNumber = "30004505000017",
            VerificationCodeTemplateId = "template-456"
        };

        // Assert
        Assert.Equal("api-key-123", options.Key);
        Assert.Equal("30004505000017", options.LineNumber);
        Assert.Equal("template-456", options.VerificationCodeTemplateId);
    }

    [Fact]
    public void SmsDotIrOptions_RecordEquality_Works()
    {
        // Arrange
        SmsDotIrOptions options1 = new() { Key = "key1", LineNumber = "line1", VerificationCodeTemplateId = "template1" };
        SmsDotIrOptions options2 = new() { Key = "key1", LineNumber = "line1", VerificationCodeTemplateId = "template1" };
        SmsDotIrOptions options3 = new() { Key = "key2", LineNumber = "line1", VerificationCodeTemplateId = "template1" };

        // Assert
        Assert.Equal(options1, options2);
        Assert.NotEqual(options1, options3);
    }

    #endregion

    #region SmtpOptions Tests

    [Fact]
    public void SmtpOptions_HasDefaultValues()
    {
        // Arrange & Act
        SmtpOptions options = new();

        // Assert
        Assert.Equal(string.Empty, options.Host);
        Assert.Equal(0, options.Port);
        Assert.False(options.EnableSsl);
        Assert.Equal(string.Empty, options.Name);
        Assert.Equal(string.Empty, options.Username);
        Assert.Equal(string.Empty, options.Password);
    }

    [Fact]
    public void SmtpOptions_CanSetAllProperties()
    {
        // Arrange & Act
        SmtpOptions options = new()
        {
            Host = "smtp.example.com",
            Port = 587,
            EnableSsl = true,
            Name = "Test Sender",
            Username = "user@example.com",
            Password = "secretPassword123"
        };

        // Assert
        Assert.Equal("smtp.example.com", options.Host);
        Assert.Equal(587, options.Port);
        Assert.True(options.EnableSsl);
        Assert.Equal("Test Sender", options.Name);
        Assert.Equal("user@example.com", options.Username);
        Assert.Equal("secretPassword123", options.Password);
    }

    [Theory]
    [InlineData(25)]
    [InlineData(465)]
    [InlineData(587)]
    [InlineData(2525)]
    public void SmtpOptions_AcceptsVariousPortValues(int port)
    {
        // Arrange & Act
        SmtpOptions options = new() { Port = port };

        // Assert
        Assert.Equal(port, options.Port);
    }

    #endregion

    #region ProvidersOptions Tests

    [Fact]
    public void ProvidersOptions_HasDefaultValues()
    {
        // Arrange & Act
        ProvidersOptions options = new();

        // Assert
        Assert.NotNull(options.SmsDotIr);
        Assert.NotNull(options.Smtp);
    }

    [Fact]
    public void ProvidersOptions_CanSetAllProviders()
    {
        // Arrange & Act
        ProvidersOptions options = new()
        {
            SmsDotIr = new SmsDotIrOptions { Key = "sms-key" },
            Smtp = new SmtpOptions { Host = "smtp.host.com" }
        };

        // Assert
        Assert.Equal("sms-key", options.SmsDotIr.Key);
        Assert.Equal("smtp.host.com", options.Smtp.Host);
    }

    #endregion

    #region NotificationOptions Tests

    [Fact]
    public void NotificationOptions_HasDefaultValues()
    {
        // Arrange & Act
        NotificationOptions options = new();

        // Assert
        Assert.Equal(string.Empty, options.StartupSmsRecipient);
        Assert.Equal(string.Empty, options.HealthFailureSmsRecipient);
    }

    [Fact]
    public void NotificationOptions_CanSetAllProperties()
    {
        // Arrange & Act
        NotificationOptions options = new()
        {
            StartupSmsRecipient = "+15551234567",
            HealthFailureSmsRecipient = "+15559876543"
        };

        // Assert
        Assert.Equal("+15551234567", options.StartupSmsRecipient);
        Assert.Equal("+15559876543", options.HealthFailureSmsRecipient);
    }

    #endregion
}
