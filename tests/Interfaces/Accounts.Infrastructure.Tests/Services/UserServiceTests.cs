using Microsoft.AspNetCore.Identity;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Accounts.Infrastructure.Tests.TestInfrastructure;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Accounts.Infrastructure.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task UserService_GetById_ReturnsUser_WhenFound()
    {
        FakeDistributedCache cache = new();
        Mock<IUsersService> usersService = new();
        Mock<IUserProfileService> profileService = new();
        Mock<IEmailAuthenticationService> emailAuthService = new();
        Mock<ISmsAuthenticationService> smsAuthService = new();
        User expected = new() { Id = "user-123", UserName = "test" };

        usersService.Setup(service => service.GetUserAsync("user-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        UserService sut = new(
            usersService.Object,
            profileService.Object,
            emailAuthService.Object,
            smsAuthService.Object,
            cache);

        User? first = await sut.GetUserAsync("user-123");
        User? second = await sut.GetUserAsync("user-123");

        first.Should().NotBeNull();
        first.Should().BeEquivalentTo(expected);
        second.Should().BeEquivalentTo(expected);
        usersService.Verify(service => service.GetUserAsync("user-123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UserService_GetById_ReturnsNull_WhenMissing()
    {
        FakeDistributedCache cache = new();
        Mock<IUsersService> usersService = new();
        Mock<IUserProfileService> profileService = new();
        Mock<IEmailAuthenticationService> emailAuthService = new();
        Mock<ISmsAuthenticationService> smsAuthService = new();

        usersService.Setup(service => service.GetUserAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        UserService sut = new(
            usersService.Object,
            profileService.Object,
            emailAuthService.Object,
            smsAuthService.Object,
            cache);

        User? first = await sut.GetUserAsync("missing");
        User? second = await sut.GetUserAsync("missing");

        first.Should().BeNull();
        second.Should().BeNull();
        usersService.Verify(service => service.GetUserAsync("missing", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UserService_CreateUser_Succeeds_ReturnsResult()
    {
        FakeDistributedCache cache = new();
        Mock<IUsersService> usersService = new();
        Mock<IUserProfileService> profileService = new();
        Mock<IEmailAuthenticationService> emailAuthService = new();
        Mock<ISmsAuthenticationService> smsAuthService = new();
        User user = new() { Email = "hello@example.com", FirstName = "A", LastName = "B" };

        usersService.Setup(service => service.CreateUserWithEmailAsync(It.IsAny<CreateUserWithEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        UserService sut = new(
            usersService.Object,
            profileService.Object,
            emailAuthService.Object,
            smsAuthService.Object,
            cache);

        IdentityResult result = await sut.CreateUserAsync(user);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UserService_CreateUser_Fails_ReturnsErrors()
    {
        FakeDistributedCache cache = new();
        Mock<IUsersService> usersService = new();
        Mock<IUserProfileService> profileService = new();
        Mock<IEmailAuthenticationService> emailAuthService = new();
        Mock<ISmsAuthenticationService> smsAuthService = new();
        User user = new() { Email = "fail@example.com" };

        usersService.Setup(service => service.CreateUserWithEmailAsync(It.IsAny<CreateUserWithEmailCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        UserService sut = new(
            usersService.Object,
            profileService.Object,
            emailAuthService.Object,
            smsAuthService.Object,
            cache);

        IdentityResult result = await sut.CreateUserAsync(user);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "CreateUserFailed");
    }
}
