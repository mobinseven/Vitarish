using Microsoft.AspNetCore.Identity;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Accounts.Infrastructure.Tests.TestInfrastructure;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Accounts.Infrastructure.Tests.Services;

public class EmailAuthenticationServiceTests
{
    [Fact]
    public async Task EmailAuthenticationService_SendCode_QueuesMessage()
    {
        Mock<UserManager<User>> userManager = UserManagerFactory.CreateMock();
        Mock<IMessageQueue> messageQueue = new();
        Mock<ILogger<EmailAuthenticationService>> logger = new();
        User user = new() { UserName = "tester" };

        userManager.Setup(manager => manager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider))
            .ReturnsAsync("123456");

        EmailAuthenticationService sut = new(userManager.Object, logger.Object, messageQueue.Object);

        await sut.SendCode(user, "user@example.com");

        messageQueue.Verify(queue => queue.EnqueueEmail(
            "user@example.com",
            "Code",
            It.Is<string>(text => text.Contains("123456", StringComparison.Ordinal)),
            It.Is<string>(html => html.Contains("123456", StringComparison.Ordinal))),
            Times.Once);
    }

    [Fact]
    public async Task EmailAuthenticationService_SendCode_InvalidEmail_ThrowsOrReturnsFailure()
    {
        Mock<UserManager<User>> userManager = UserManagerFactory.CreateMock();
        Mock<IMessageQueue> messageQueue = new();
        Mock<ILogger<EmailAuthenticationService>> logger = new();
        User user = new();

        EmailAuthenticationService sut = new(userManager.Object, logger.Object, messageQueue.Object);

        Func<Task> act = () => sut.SendCode(user, string.Empty);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EmailAuthenticationService_VerifyCode_Succeeds_WhenValid()
    {
        Mock<UserManager<User>> userManager = UserManagerFactory.CreateMock();
        Mock<IMessageQueue> messageQueue = new();
        Mock<ILogger<EmailAuthenticationService>> logger = new();
        User user = new();

        userManager.Setup(manager => manager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, "code"))
            .ReturnsAsync(true);

        EmailAuthenticationService sut = new(userManager.Object, logger.Object, messageQueue.Object);

        bool result = await sut.VerifyCode(user, "code");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task EmailAuthenticationService_VerifyCode_Fails_WhenInvalidOrExpired()
    {
        Mock<UserManager<User>> userManager = UserManagerFactory.CreateMock();
        Mock<IMessageQueue> messageQueue = new();
        Mock<ILogger<EmailAuthenticationService>> logger = new();
        User user = new();

        userManager.Setup(manager => manager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider, "code"))
            .ReturnsAsync(false);

        EmailAuthenticationService sut = new(userManager.Object, logger.Object, messageQueue.Object);

        bool result = await sut.VerifyCode(user, "code");

        result.Should().BeFalse();
    }
}
