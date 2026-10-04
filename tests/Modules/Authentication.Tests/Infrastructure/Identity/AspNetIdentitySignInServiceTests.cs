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
