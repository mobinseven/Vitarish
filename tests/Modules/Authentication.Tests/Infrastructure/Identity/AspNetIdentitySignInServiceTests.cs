using Vitarish.Authentication.Domain.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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

    [Fact]
    public async Task A_disable_that_lands_after_the_status_check_revokes_the_new_session()
    {
        User staleEnabledUser = CreateUser(isEnabled: true);
        User storedUser = CreateUser(isEnabled: false);
        storedUser.Id = staleEnabledUser.Id;
        await using AccountStatusContext statusContext = new();
        _ = statusContext.Users.Add(storedUser);
        _ = await statusContext.SaveChangesAsync();

        Mock<IUserStore<User>> userStore = new();
        Mock<UserManager<User>> userManager = CreateUserManager(userStore);
        userManager.Setup(manager => manager.FindByIdAsync(staleEnabledUser.Id)).ReturnsAsync(staleEnabledUser);
        userManager.SetupGet(manager => manager.Users).Returns(statusContext.Users);
        Mock<SignInManager<User>> signInManager = CreateSignInManager(userManager.Object);
        signInManager.Setup(manager => manager.CreateUserPrincipalAsync(staleEnabledUser))
            .ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity("test")));
        Mock<IAuthenticationSessionPreparation> preparation = new();
        preparation.SetupGet(value => value.Result).Returns(new AuthenticationSessionEstablishmentResult(true, "session-1", null, null, 30));
        Mock<IAuthenticationSessionRegistry> sessionRegistry = new();
        sessionRegistry.Setup(registry => registry.PrepareAsync(staleEnabledUser.Id, It.IsAny<CancellationToken>())).ReturnsAsync(preparation.Object);
        Mock<IAuthenticationService> authentication = new();
        DefaultHttpContext httpContext = new()
        {
            RequestServices = new ServiceCollection().AddSingleton(authentication.Object).BuildServiceProvider()
        };

        AspNetIdentitySignInService service = CreateService(
            userManager,
            signInManager,
            new Mock<IAuthenticationAttemptTracker>(),
            sessionRegistry,
            new Mock<IAuthenticationAttemptKeyFactory>(),
            new HttpContextAccessor { HttpContext = httpContext });

        AuthenticationSessionSignInResult result = await service.SignInAsync(staleEnabledUser.Id, rememberMe: false, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Failure!.Reason.Should().Be(AuthenticationFailureReason.AccountDisabled);
        preparation.Verify(value => value.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        sessionRegistry.Verify(
            registry => registry.RevokeAsync("session-1", AuthenticationSessionTerminationReason.SecurityAttributeChanged, It.IsAny<CancellationToken>()),
            Times.Once);
        authentication.Verify(
            value => value.SignOutAsync(httpContext, IdentityConstants.ApplicationScheme, It.IsAny<AuthenticationProperties?>()),
            Times.Once);
    }

    [Fact]
    public async Task Session_sign_in_rejects_an_account_without_an_access_level_before_preparing_a_session()
    {
        User user = CreateUser(isEnabled: true);
        Mock<IUserStore<User>> userStore = new();
        Mock<UserManager<User>> userManager = CreateUserManager(userStore);
        userManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(manager => manager.GetRolesAsync(user)).ReturnsAsync(Array.Empty<string>());
        Mock<IAuthenticationSessionRegistry> sessionRegistry = new();

        AspNetIdentitySignInService service = CreateService(
            userManager,
            CreateSignInManager(userManager.Object),
            new Mock<IAuthenticationAttemptTracker>(),
            sessionRegistry,
            new Mock<IAuthenticationAttemptKeyFactory>());

        AuthenticationSessionSignInResult result = await service.SignInAsync(user.Id, rememberMe: false, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Failure!.Reason.Should().Be(AuthenticationFailureReason.NoAccess);
        sessionRegistry.Verify(
            registry => registry.PrepareAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Password_sign_in_reports_missing_access_only_after_the_password_verifies()
    {
        User user = CreateUser(isEnabled: true);
        Mock<IUserStore<User>> userStore = new();
        Mock<UserManager<User>> userManager = CreateUserManager(userStore);
        userManager.Setup(manager => manager.FindByNameAsync(user.UserName!)).ReturnsAsync(user);
        userManager.Setup(manager => manager.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        userManager.Setup(manager => manager.FindByIdAsync(user.Id)).ReturnsAsync(user);
        userManager.Setup(manager => manager.IsLockedOutAsync(user)).ReturnsAsync(false);
        userManager.Setup(manager => manager.GetRolesAsync(user)).ReturnsAsync(Array.Empty<string>());
        Mock<SignInManager<User>> signInManager = CreateSignInManager(userManager.Object);
        signInManager
            .Setup(manager => manager.CheckPasswordSignInAsync(user, "correct-password", false))
            .ReturnsAsync(SignInResult.Success);
        Mock<IAuthenticationSessionRegistry> sessionRegistry = new();

        AspNetIdentitySignInService service = CreateService(
            userManager,
            signInManager,
            CreateAttemptTracker(),
            sessionRegistry,
            CreateAttemptKeyFactory(user));

        PasswordSignInResult result = await service.PasswordSignInAsync(user.UserName!, "correct-password", rememberMe: false, CancellationToken.None);

        result.Status.Should().Be(PasswordSignInStatus.NoAccess);
        result.UserId.Should().Be(user.Id);
        sessionRegistry.Verify(
            registry => registry.PrepareAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Wrong_credentials_for_an_account_without_access_are_indistinguishable_from_an_account_with_access()
    {
        User user = CreateUser(isEnabled: true);
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

        userManager.Setup(manager => manager.GetRolesAsync(user)).ReturnsAsync(Array.Empty<string>());
        PasswordSignInResult noAccessResult = await service.PasswordSignInAsync(user.UserName!, "wrong-password", rememberMe: false, CancellationToken.None);
        userManager.Setup(manager => manager.GetRolesAsync(user)).ReturnsAsync(new List<string> { IdentityDefinitions.PlatformAdmin });
        PasswordSignInResult accessResult = await service.PasswordSignInAsync(user.UserName!, "wrong-password", rememberMe: false, CancellationToken.None);

        noAccessResult.Status.Should().Be(PasswordSignInStatus.InvalidCredentials);
        accessResult.Status.Should().Be(noAccessResult.Status);
    }

    [Fact]
    public async Task Access_lost_after_the_status_check_revokes_the_new_session()
    {
        User staleAccessUser = CreateUser(isEnabled: true);
        User storedUser = CreateUser(isEnabled: true);
        storedUser.Id = staleAccessUser.Id;
        await using AccountStatusContext statusContext = new();
        _ = statusContext.Users.Add(storedUser);
        _ = await statusContext.SaveChangesAsync();

        Mock<IUserStore<User>> userStore = new();
        Mock<UserManager<User>> userManager = CreateUserManager(userStore);
        userManager.Setup(manager => manager.FindByIdAsync(staleAccessUser.Id)).ReturnsAsync(staleAccessUser);
        userManager.SetupGet(manager => manager.Users).Returns(statusContext.Users);
        userManager.SetupSequence(manager => manager.GetRolesAsync(staleAccessUser))
            .ReturnsAsync(new List<string> { IdentityDefinitions.PlatformAdmin })
            .ReturnsAsync(Array.Empty<string>());
        Mock<SignInManager<User>> signInManager = CreateSignInManager(userManager.Object);
        signInManager.Setup(manager => manager.CreateUserPrincipalAsync(staleAccessUser))
            .ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity("test")));
        Mock<IAuthenticationSessionPreparation> preparation = new();
        preparation.SetupGet(value => value.Result).Returns(new AuthenticationSessionEstablishmentResult(true, "session-1", null, null, 30));
        Mock<IAuthenticationSessionRegistry> sessionRegistry = new();
        sessionRegistry.Setup(registry => registry.PrepareAsync(staleAccessUser.Id, It.IsAny<CancellationToken>())).ReturnsAsync(preparation.Object);
        Mock<IAuthenticationService> authentication = new();
        DefaultHttpContext httpContext = new()
        {
            RequestServices = new ServiceCollection().AddSingleton(authentication.Object).BuildServiceProvider()
        };

        AspNetIdentitySignInService service = CreateService(
            userManager,
            signInManager,
            new Mock<IAuthenticationAttemptTracker>(),
            sessionRegistry,
            new Mock<IAuthenticationAttemptKeyFactory>(),
            new HttpContextAccessor { HttpContext = httpContext });

        AuthenticationSessionSignInResult result = await service.SignInAsync(staleAccessUser.Id, rememberMe: false, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Failure!.Reason.Should().Be(AuthenticationFailureReason.NoAccess);
        preparation.Verify(value => value.CompleteAsync(It.IsAny<CancellationToken>()), Times.Once);
        sessionRegistry.Verify(
            registry => registry.RevokeAsync("session-1", AuthenticationSessionTerminationReason.SecurityAttributeChanged, It.IsAny<CancellationToken>()),
            Times.Once);
        authentication.Verify(
            value => value.SignOutAsync(httpContext, IdentityConstants.ApplicationScheme, It.IsAny<AuthenticationProperties?>()),
            Times.Once);
    }

    private sealed class AccountStatusContext : DbContext
    {
        public DbSet<User> Users => Set<User>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseInMemoryDatabase(Guid.NewGuid().ToString("N"));
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
        Mock<IAuthenticationAttemptKeyFactory> attemptKeyFactory,
        IHttpContextAccessor? httpContextAccessor = null)
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
            httpContextAccessor ?? new HttpContextAccessor());
    }

    private static Mock<UserManager<User>> CreateUserManager(Mock<IUserStore<User>> userStore)
    {
        Mock<UserManager<User>> userManager = new(
            userStore.Object,
            Options.Create(new IdentityOptions()),
            Mock.Of<IPasswordHasher<User>>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            NullLogger<UserManager<User>>.Instance);
        userManager.Setup(manager => manager.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { IdentityDefinitions.PlatformAdmin });
        return userManager;
    }

    private static Mock<SignInManager<User>> CreateSignInManager(UserManager<User> userManager) => new(
        userManager,
        new HttpContextAccessor(),
        Mock.Of<IUserClaimsPrincipalFactory<User>>(),
        Options.Create(new IdentityOptions()),
        NullLogger<SignInManager<User>>.Instance,
        Mock.Of<IAuthenticationSchemeProvider>(),
        Mock.Of<IUserConfirmation<User>>());
}
