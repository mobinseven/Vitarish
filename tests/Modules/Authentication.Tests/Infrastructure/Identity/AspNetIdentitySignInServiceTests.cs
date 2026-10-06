using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using FluentAssertions;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Tests.Infrastructure.Identity;

public sealed class AspNetIdentitySignInServiceTests
{
    [Theory]
    [InlineData(AuthenticationFailureReason.NotAllowed, PasswordSignInStatus.NotAllowed)]
    [InlineData(AuthenticationFailureReason.LockedOut, PasswordSignInStatus.LockedOut)]
    [InlineData(AuthenticationFailureReason.TooManyAttempts, PasswordSignInStatus.TooManyAttempts)]
    [InlineData(AuthenticationFailureReason.ProviderFailure, PasswordSignInStatus.ProviderFailure)]
    public async Task PasswordSignInAsync_preserves_the_session_admission_failure_category(
        AuthenticationFailureReason failureReason,
        PasswordSignInStatus expectedStatus)
    {
        User user = new()
        {
            Id = "session-admission-user",
            UserName = "session-admission-user",
            Email = "session-admission-user@example.test",
            FirstName = "Session",
            LastName = "Admission"
        };
        Mock<IUserStore<User>> userStore = new();
        Mock<UserManager<User>> userManager = CreateUserManager(userStore);
        userManager.Setup(manager => manager.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        userManager.Setup(manager => manager.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(false);

        Mock<SignInManager<User>> signInManager = CreateSignInManager(userManager.Object);
        signInManager
            .Setup(manager => manager.CheckPasswordSignInAsync(user, It.IsAny<string>(), false))
            .ReturnsAsync(SignInResult.Success);

        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);

        Mock<IAuthenticationAttemptTracker> attemptTracker = new();
        attemptTracker.Setup(tracker => tracker.IsDeniedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        attemptTracker.Setup(tracker => tracker.ResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        Mock<IAuthenticationAttemptKeyFactory> attemptKeyFactory = new();
        attemptKeyFactory.Setup(factory => factory.IdentifierCandidates(AuthenticationMethod.Password, user.UserName!)).Returns(["identifier-key"]);
        attemptKeyFactory.Setup(factory => factory.SubjectCandidates(AuthenticationMethod.Password, user.Id)).Returns(["subject-key"]);

        Mock<IAuthenticationSessionPreparation> preparation = new();
        preparation.SetupGet(item => item.Result).Returns(AuthenticationSessionEstablishmentResult.Failed(
            new AuthenticationFailure(failureReason, "Session admission failed.")));
        preparation.Setup(item => item.DisposeAsync()).Returns(ValueTask.CompletedTask);

        Mock<IAuthenticationSessionRegistry> sessionRegistry = new();
        sessionRegistry.Setup(registry => registry.PrepareAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(preparation.Object);

        AspNetIdentitySignInService service = new(
            userManager.Object,
            signInManager.Object,
            policyProvider.Object,
            attemptTracker.Object,
            new UnknownPasswordVerifier(new PasswordHasher<User>()),
            attemptKeyFactory.Object,
            sessionRegistry.Object,
            new HttpContextAccessor());

        PasswordSignInResult result = await service.PasswordSignInAsync(user.UserName!, "unused-password", false, CancellationToken.None);

        result.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task Session_sign_in_rejects_a_disabled_account_before_preparing_a_session()
    {
        User user = CreateUser(isEnabled: false);
        Mock<IUserStore<User>> userStore = new();
        Mock<UserManager<User>> userManager = CreateUserManager(userStore);
        userManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);
        Mock<IAuthenticationSessionRegistry> sessionRegistry = new();

        AspNetIdentitySignInService service = CreateService(
            userManager,
            CreateSignInManager(userManager.Object),
            new Mock<IAuthenticationAttemptTracker>(),
            sessionRegistry,
            new Mock<IAuthenticationAttemptKeyFactory>());

        AuthenticationSessionSignInResult result = await service.SignInAsync(user.Id, rememberMe: false, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Failure!.Reason.Should().Be(AuthenticationFailureReason.AccountDisabled);
        sessionRegistry.Verify(
            registry => registry.PrepareAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Password_sign_in_reports_the_disabled_state_only_after_the_password_verifies()
    {
        User user = CreateUser(isEnabled: false);
        Mock<IUserStore<User>> userStore = new();
        Mock<UserManager<User>> userManager = CreateUserManager(userStore);
        userManager.Setup(manager => manager.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        userManager.Setup(manager => manager.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(false);
        Mock<SignInManager<User>> signInManager = CreateSignInManager(userManager.Object);
        signInManager
            .Setup(manager => manager.CheckPasswordSignInAsync(user, "correct-password", false))
            .ReturnsAsync(SignInResult.Success);
        Mock<IAuthenticationAttemptTracker> attemptTracker = CreateAttemptTracker();
        Mock<IAuthenticationSessionRegistry> sessionRegistry = new();

        AspNetIdentitySignInService service = CreateService(
            userManager,
            signInManager,
            attemptTracker,
            sessionRegistry,
            CreateAttemptKeyFactory(user));

        PasswordSignInResult result = await service.PasswordSignInAsync(user.UserName!, "correct-password", rememberMe: false, CancellationToken.None);

        result.Status.Should().Be(PasswordSignInStatus.AccountDisabled);
        result.UserId.Should().Be(user.Id);
        sessionRegistry.Verify(
            registry => registry.PrepareAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Wrong_credentials_for_a_disabled_account_are_indistinguishable_from_an_enabled_account()
    {
        User user = CreateUser(isEnabled: false);
        Mock<IUserStore<User>> userStore = new();
        Mock<UserManager<User>> userManager = CreateUserManager(userStore);
        userManager.Setup(manager => manager.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        userManager.Setup(manager => manager.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(false);
        Mock<SignInManager<User>> signInManager = CreateSignInManager(userManager.Object);
        signInManager
            .Setup(manager => manager.CheckPasswordSignInAsync(user, "wrong-password", false))
            .ReturnsAsync(SignInResult.Failed);
        Mock<IAuthenticationAttemptTracker> attemptTracker = CreateAttemptTracker();
        attemptTracker
            .Setup(tracker => tracker.RegisterFailureAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        AspNetIdentitySignInService service = CreateService(
            userManager,
            signInManager,
            attemptTracker,
            new Mock<IAuthenticationSessionRegistry>(),
            CreateAttemptKeyFactory(user));

        PasswordSignInResult disabledResult = await service.PasswordSignInAsync(user.UserName!, "wrong-password", rememberMe: false, CancellationToken.None);
        user.IsEnabled = true;
        PasswordSignInResult enabledResult = await service.PasswordSignInAsync(user.UserName!, "wrong-password", rememberMe: false, CancellationToken.None);

        disabledResult.Status.Should().Be(PasswordSignInStatus.InvalidCredentials);
        enabledResult.Status.Should().Be(disabledResult.Status);
    }

    [Fact]
    public async Task Password_plus_phone_otp_check_reports_disabled_instead_of_requiring_the_second_factor()
    {
        User user = CreateUser(isEnabled: false);
        user.TwoFactorEnabled = true;
        Mock<IUserStore<User>> userStore = new();
        Mock<UserManager<User>> userManager = CreateUserManager(userStore);
        userManager.Setup(manager => manager.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        userManager.Setup(manager => manager.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(false);
        Mock<SignInManager<User>> signInManager = CreateSignInManager(userManager.Object);
        signInManager
            .Setup(manager => manager.CheckPasswordSignInAsync(user, "correct-password", false))
            .ReturnsAsync(SignInResult.TwoFactorRequired);

        AspNetIdentitySignInService service = CreateService(
            userManager,
            signInManager,
            CreateAttemptTracker(),
            new Mock<IAuthenticationSessionRegistry>(),
            CreateAttemptKeyFactory(user));

        PasswordSignInResult result = await service.CheckPasswordSignInAsync(user.UserName!, "correct-password", CancellationToken.None);

        result.Status.Should().Be(PasswordSignInStatus.AccountDisabled);
        result.RequiresTwoFactor.Should().BeFalse();
    }

    private static User CreateUser(bool isEnabled) => new()
    {
        Id = "managed-user",
        UserName = "managed-user@example.test",
        Email = "managed-user@example.test",
        FirstName = "Managed",
        LastName = "User",
        IsEnabled = isEnabled
    };

    private static Mock<IAuthenticationAttemptTracker> CreateAttemptTracker()
    {
        Mock<IAuthenticationAttemptTracker> attemptTracker = new();
        attemptTracker.Setup(tracker => tracker.IsDeniedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        attemptTracker.Setup(tracker => tracker.ResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return attemptTracker;
    }

    private static Mock<IAuthenticationAttemptKeyFactory> CreateAttemptKeyFactory(User user)
    {
        Mock<IAuthenticationAttemptKeyFactory> attemptKeyFactory = new();
        attemptKeyFactory.Setup(factory => factory.IdentifierCandidates(AuthenticationMethod.Password, user.UserName!)).Returns(["identifier-key"]);
        attemptKeyFactory.Setup(factory => factory.SubjectCandidates(AuthenticationMethod.Password, user.Id)).Returns(["subject-key"]);
        return attemptKeyFactory;
    }

    private static AspNetIdentitySignInService CreateService(
        Mock<UserManager<User>> userManager,
        Mock<SignInManager<User>> signInManager,
        Mock<IAuthenticationAttemptTracker> attemptTracker,
        Mock<IAuthenticationSessionRegistry> sessionRegistry,
        Mock<IAuthenticationAttemptKeyFactory> attemptKeyFactory)
    {
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);

        return new AspNetIdentitySignInService(
            userManager.Object,
            signInManager.Object,
            policyProvider.Object,
            attemptTracker.Object,
            new UnknownPasswordVerifier(new PasswordHasher<User>()),
            attemptKeyFactory.Object,
            sessionRegistry.Object,
            new HttpContextAccessor());
    }

    private static Mock<UserManager<User>> CreateUserManager(Mock<IUserStore<User>> userStore) => new(
        userStore.Object,
        Options.Create(new IdentityOptions()),
        Mock.Of<IPasswordHasher<User>>(),
        Array.Empty<IUserValidator<User>>(),
        Array.Empty<IPasswordValidator<User>>(),
        Mock.Of<ILookupNormalizer>(),
        new IdentityErrorDescriber(),
        Mock.Of<IServiceProvider>(),
        NullLogger<UserManager<User>>.Instance);

    private static Mock<SignInManager<User>> CreateSignInManager(UserManager<User> userManager) => new(
        userManager,
        new HttpContextAccessor(),
        Mock.Of<IUserClaimsPrincipalFactory<User>>(),
        Options.Create(new IdentityOptions()),
        NullLogger<SignInManager<User>>.Instance,
        Mock.Of<IAuthenticationSchemeProvider>(),
        Mock.Of<IUserConfirmation<User>>());
}
