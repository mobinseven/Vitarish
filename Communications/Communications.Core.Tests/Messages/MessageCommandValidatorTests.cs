using FluentAssertions;
using Vitarish.Communications.Core.Messages.Application.Commands;

namespace Communications.Core.Tests.Messages;

public class SendSmsValidatorTests
{
    private readonly SendSmsValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new SendSms(Recipient: "+10000000000", Body: "Hello");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Hello")]
    [InlineData(null, "Hello")]
    public void Validate_WithEmptyOrNullRecipient_ReturnsInvalid(string? recipient, string body)
    {
        // Arrange
        var command = new SendSms(Recipient: recipient!, Body: body);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Theory]
    [InlineData("+10000000000", "")]
    [InlineData("+10000000000", null)]
    public void Validate_WithEmptyOrNullBody_ReturnsInvalid(string recipient, string? body)
    {
        // Arrange
        var command = new SendSms(Recipient: recipient, Body: body!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Body");
    }
}

public class SendSmsCodeValidatorTests
{
    private readonly SendSmsCodeValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new SendSmsCode(Recipient: "+10000000000", Code: "123456");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "123456")]
    [InlineData(null, "123456")]
    public void Validate_WithEmptyOrNullRecipient_ReturnsInvalid(string? recipient, string code)
    {
        // Arrange
        var command = new SendSmsCode(Recipient: recipient!, Code: code);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }

    [Theory]
    [InlineData("+10000000000", "")]
    [InlineData("+10000000000", null)]
    public void Validate_WithEmptyOrNullCode_ReturnsInvalid(string recipient, string? code)
    {
        // Arrange
        var command = new SendSmsCode(Recipient: recipient, Code: code!);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Code");
    }
}

public class SendEmailValidatorTests
{
    private readonly SendEmailValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsValid()
    {
        // Arrange
        var command = new SendEmail(
            Recipient: "test@example.com",
            Subject: "Test Subject",
            Body: "Test Body",
            HtmlBody: "<p>Test Body</p>");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithOnlyRecipient_ReturnsValid()
    {
        // Arrange
        var command = new SendEmail(
            Recipient: "test@example.com",
            Subject: null,
            Body: null,
            HtmlBody: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyOrNullRecipient_ReturnsInvalid(string? recipient)
    {
        // Arrange
        var command = new SendEmail(
            Recipient: recipient!,
            Subject: "Test Subject",
            Body: "Test Body",
            HtmlBody: null);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Recipient");
    }
}
