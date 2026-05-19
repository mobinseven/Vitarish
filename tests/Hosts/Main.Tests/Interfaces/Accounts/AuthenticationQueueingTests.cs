using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Vitarish.Communications.Application.Abstractions;
using Vitarish.Interfaces.Accounts.Services;
using Vitarish.Users.Domain.Entities;

namespace Main.Tests.Interfaces.Accounts;

public class AuthenticationQueueingTests
{
    [Fact]
    public async Task EmailAuthenticationService_SendCode_EnqueuesEmailCommand()
    {
        User user = new() { UserName = "tester" };
        Mock<UserManager<User>> userManager = CreateUserManagerMock();
        Mock<ICommunicationCommandQueue> queue = new();

        _ = userManager
            .Setup(x => x.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider))
            .ReturnsAsync("654321");

        EmailAuthenticationService sut = new(
            userManager.Object,
            Mock.Of<ILogger<EmailAuthenticationService>>(),
            queue.Object);

        await sut.SendCode(user, "test@example.com");

        queue.Verify(
            x => x.Enqueue(It.Is<EmailCommunicationCommand>(command =>
                command.Recipient == "test@example.com" &&
                command.Subject == "Code" &&
                !string.IsNullOrWhiteSpace(command.TextBody) &&
                !string.IsNullOrWhiteSpace(command.HtmlBody))),
            Times.Once);
    }

    [Fact]
    public async Task SmsAuthenticationService_SendCode_EnqueuesSmsCodeCommand()
    {
        User user = new();
        Mock<UserManager<User>> userManager = CreateUserManagerMock();
        Mock<ICommunicationCommandQueue> queue = new();

        _ = userManager
            .Setup(x => x.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider))
            .ReturnsAsync("123456");

        SmsAuthenticationService sut = new(
            userManager.Object,
            Mock.Of<ILogger<SmsAuthenticationService>>(),
            queue.Object);

        await sut.SendCode(user, "+1234567890");

        queue.Verify(
            x => x.Enqueue(It.Is<SmsCodeCommunicationCommand>(command =>
                command.Recipient == "+1234567890" &&
                command.Code == "123456")),
            Times.Once);
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        Mock<IUserStore<User>> userStoreMock = new();
        return new Mock<UserManager<User>>(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }
}
