namespace Communications.Infrastructure.Tests;

using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Vitarish.Communications.Core.Messages.Abstractions;
using Vitarish.Communications.Core.Messages.Models;
using Vitarish.Communications.Infrastructure.Messages.Options;
using Vitarish.Communications.Infrastructure.Messages.Services;

public class MessagesServiceTests
{
    [Fact]
    public async Task SendSmsAsync_DelegatesCallToSmsProvider()
    {
        var expected = new SmsSendResult("123", "+1234567890", 1.0m, Guid.NewGuid(), null, null, null);
        var message = new Message { Recipient = "+1234567890", Body = "Test" };
        var smsProvider = Substitute.For<ISmsProvider>();
        smsProvider.SendSmsAsync(message, Arg.Any<CancellationToken>()).Returns(expected);

        var options = Options.Create(new CommunicationsOptions());
        var service = new MessagesService(options, smsProvider);

        var result = await service.SendSmsAsync(message);

        result.Should().Be(expected);
        await smsProvider.Received(1).SendSmsAsync(message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendSmsCodeAsync_DelegatesCallToSmsProvider()
    {
        var message = new Message { Recipient = "+1234567890", Body = "123456" };
        var smsProvider = Substitute.For<ISmsProvider>();

        var options = Options.Create(new CommunicationsOptions());
        var service = new MessagesService(options, smsProvider);

        await service.SendSmsCodeAsync(message);

        await smsProvider.Received(1).SendCodeAsync(message, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSmsDeliveryStateAsync_DelegatesCallToSmsProvider()
    {
        var expected = new SmsDeliveryState("123", "+1234567890", 1, "Delivered", "Test", 1.0m, null, null, null);
        var smsProvider = Substitute.For<ISmsProvider>();
        smsProvider.GetSmsDeliveryStateAsync(123, Arg.Any<CancellationToken>()).Returns(expected);

        var options = Options.Create(new CommunicationsOptions());
        var service = new MessagesService(options, smsProvider);

        var result = await service.GetSmsDeliveryStateAsync(123);

        result.Should().Be(expected);
        await smsProvider.Received(1).GetSmsDeliveryStateAsync(123, Arg.Any<CancellationToken>());
    }
}
