using Microsoft.AspNetCore.Identity;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Accounts.Infrastructure.Tests.TestInfrastructure;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Accounts.Infrastructure.Tests.Services;

public class SmsAuthenticationServiceTests
{
    [Fact]
    public async Task SmsAuthenticationService_SendCode_QueuesMessage()
    {
        Mock<UserManager<User>> userManager = UserManagerFactory.CreateMock();
        Mock<IMessageQueue> messageQueue = new();
        Mock<ILogger<SmsAuthenticationService>> logger = new();
        User user = new();

        userManager.Setup(manager => manager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider))
            .ReturnsAsync("654321");

        SmsAuthenticationService sut = new(userManager.Object, logger.Object, messageQueue.Object);

        await sut.SendCode(user, "+1234567890");

        messageQueue.Verify(queue => queue.EnqueueSms("+1234567890", "654321"), Times.Once);
    }

    [Fact]
    public async Task SmsAuthenticationService_SendCode_InvalidPhone_ThrowsOrReturnsFailure()
    {
        Mock<UserManager<User>> userManager = UserManagerFactory.CreateMock();
        Mock<IMessageQueue> messageQueue = new();
        Mock<ILogger<SmsAuthenticationService>> logger = new();
        User user = new();

        SmsAuthenticationService sut = new(userManager.Object, logger.Object, messageQueue.Object);

        Func<Task> act = () => sut.SendCode(user, " ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SmsAuthenticationService_VerifyCode_Succeeds_WhenValid()
    {
        Mock<UserManager<User>> userManager = UserManagerFactory.CreateMock();
        Mock<IMessageQueue> messageQueue = new();
        Mock<ILogger<SmsAuthenticationService>> logger = new();
        User user = new();

        userManager.Setup(manager => manager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider, "code"))
            .ReturnsAsync(true);

        SmsAuthenticationService sut = new(userManager.Object, logger.Object, messageQueue.Object);

        bool result = await sut.VerifyCode(user, "code");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SmsAuthenticationService_VerifyCode_Fails_WhenInvalidOrExpired()
    {
        Mock<UserManager<User>> userManager = UserManagerFactory.CreateMock();
        Mock<IMessageQueue> messageQueue = new();
        Mock<ILogger<SmsAuthenticationService>> logger = new();
        User user = new();

        userManager.Setup(manager => manager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultPhoneProvider, "code"))
            .ReturnsAsync(false);

        SmsAuthenticationService sut = new(userManager.Object, logger.Object, messageQueue.Object);

        bool result = await sut.VerifyCode(user, "code");

        result.Should().BeFalse();
    }
}
