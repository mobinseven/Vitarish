using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Tests.Security;

public sealed class AuditingSignInManagerTests
{
    [Fact]
    public async Task Every_identity_override_writes_a_redacted_denial_event()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        User user = new() { Id = "subject", UserName = "subject" };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "ValidPassword1!");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        Mock<ISecurityAuditWriter> writer = AuditWriter();
        AuditingSignInManager manager = CreateManager(dbContext, writer.Object, actorId: "actor");

        (await manager.PasswordSignInAsync(user, "wrong", false, false)).Succeeded.Should().BeFalse();
        (await manager.PasswordSignInAsync("missing", "wrong", false, false)).Succeeded.Should().BeFalse();
        (await manager.CheckPasswordSignInAsync(user, "wrong", false)).Succeeded.Should().BeFalse();
        (await manager.ExternalLoginSignInAsync("provider", "provider-key", false, false)).Succeeded.Should().BeFalse();
        (await manager.TwoFactorAuthenticatorSignInAsync("123456", false, false)).Succeeded.Should().BeFalse();
        (await manager.TwoFactorRecoveryCodeSignInAsync("recovery-code")).Succeeded.Should().BeFalse();
        (await manager.TwoFactorSignInAsync("provider", "123456", false, false)).Succeeded.Should().BeFalse();

        IReadOnlyList<SecurityAuditEvent> events = writer.Invocations.Select(invocation => invocation.Arguments[0]).Cast<SecurityAuditEvent>().ToArray();
        events.Should().HaveCount(8);
        events.Select(auditEvent => auditEvent.EventName).Should().Contain(
            [
                UsersSecurityAuditEvents.IdentityPasswordSignInUser,
                UsersSecurityAuditEvents.IdentityPasswordSignInName,
                UsersSecurityAuditEvents.IdentityPasswordCheck,
                UsersSecurityAuditEvents.IdentityExternalLogin,
                UsersSecurityAuditEvents.IdentityAuthenticatorMfa,
                UsersSecurityAuditEvents.IdentityRecoveryCodeMfa,
                UsersSecurityAuditEvents.IdentityProviderMfa
            ]);
        events
            .Should().OnlyContain(auditEvent => auditEvent.Outcome == SecurityAuditOutcome.Denied && auditEvent.TenantId == "tenant" && (auditEvent.SubjectId == null || auditEvent.SubjectId == "subject"));
        events
            .Should().OnlyContain(auditEvent => auditEvent.RedactedReason != "wrong" && auditEvent.RedactedReason != "provider-key" && auditEvent.RedactedReason != "123456" && auditEvent.RedactedReason != "recovery-code");
        events.Should().OnlyContain(auditEvent => auditEvent.ActorId == "actor" && auditEvent.CorrelationId == "request");
        events.Select(auditEvent => auditEvent.IdempotencyKey).Should().OnlyContain(idempotencyKey => idempotencyKey.StartsWith("request:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Successful_identity_result_is_preserved_in_the_audit_event()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        User user = new() { Id = "subject", UserName = "subject" };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "ValidPassword1!");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        Mock<ISecurityAuditWriter> writer = AuditWriter();
        AuditingSignInManager manager = CreateManager(dbContext, writer.Object);

        (await manager.CheckPasswordSignInAsync(user, "ValidPassword1!", false)).Should().Be(SignInResult.Success);
        writer.Invocations.Clear();
        user.TwoFactorEnabled = true;
        await dbContext.SaveChangesAsync();
        (await manager.PasswordSignInAsync(user, "ValidPassword1!", false, false)).Should().Be(SignInResult.TwoFactorRequired);

        SecurityAuditEvent[] events = writer.Invocations.Select(invocation => invocation.Arguments[0]).Cast<SecurityAuditEvent>().ToArray();
        events.Should().ContainSingle(auditEvent => auditEvent.Outcome == SecurityAuditOutcome.Pending && auditEvent.RedactedReason == SecurityAuditReasonCatalog.SecondFactorRequired);
    }

    [Fact]
    public void Two_factor_required_result_is_mapped_to_a_pending_redacted_reason()
    {
        (string outcome, string? reason) = AuditingSignInManager.Outcome(SignInResult.TwoFactorRequired, false);

        outcome.Should().Be(SecurityAuditOutcome.Pending);
        reason.Should().Be(SecurityAuditReasonCatalog.SecondFactorRequired);
    }

    [Fact]
    public async Task Audit_store_failure_does_not_change_the_identity_result()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        User user = new() { Id = "subject", UserName = "subject" };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "ValidPassword1!");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SecurityAuditStoreException("unavailable", new InvalidOperationException()));
        AuditingSignInManager manager = CreateManager(dbContext, writer.Object);

        SignInResult result = await manager.CheckPasswordSignInAsync(user, "wrong", false);

        result.Should().Be(SignInResult.Failed);
    }

    [Fact]
    public async Task Identity_exception_is_audited_as_failed_without_masking_the_exception()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        User user = new() { Id = "subject", UserName = "subject", PasswordHash = "hash" };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        Mock<ISecurityAuditWriter> writer = AuditWriter();
        InvalidOperationException providerException = new("provider failure");
        Mock<IPasswordHasher<User>> passwordHasher = new();
        passwordHasher.Setup(value => value.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>())).Throws(providerException);
        AuditingSignInManager manager = CreateManager(dbContext, writer.Object, passwordHasher.Object);

        Func<Task> act = () => manager.CheckPasswordSignInAsync(user, "wrong", false);

        await act.Should().ThrowAsync<InvalidOperationException>().Where(exception => exception == providerException);
        SecurityAuditEvent auditEvent = (SecurityAuditEvent)writer.Invocations.Single().Arguments[0];
        auditEvent.Outcome.Should().Be(SecurityAuditOutcome.Failed);
        auditEvent.RedactedReason.Should().Be(SecurityAuditReasonCatalog.AuthenticationProviderFailure);
    }

    private static AuditingSignInManager CreateManager(UsersDbContext dbContext, ISecurityAuditWriter writer, IPasswordHasher<User>? passwordHasher = null, string? actorId = null)
    {
        IdentityOptions identityOptions = new();
        identityOptions.Tokens.ProviderMap["TestTwoFactor"] = new TokenProviderDescriptor(typeof(TestTwoFactorTokenProvider));
        ServiceProvider userServices = new ServiceCollection()
            .AddSingleton<TestTwoFactorTokenProvider>()
            .AddSingleton<IUserTwoFactorTokenProvider<User>>(services => services.GetRequiredService<TestTwoFactorTokenProvider>())
            .BuildServiceProvider();
        UserManager<User> userManager = new(
            new UserStore<User>(dbContext),
            Options.Create(identityOptions),
            passwordHasher ?? new PasswordHasher<User>(),
            [new UserValidator<User>()],
            [new PasswordValidator<User>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            userServices,
            NullLogger<UserManager<User>>.Instance);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(value => value.TenantId).Returns("tenant");
        requestContext.SetupGet(value => value.UserId).Returns(actorId);
        requestContext.SetupGet(value => value.RequestId).Returns("request");
        Mock<IUserClaimsPrincipalFactory<User>> claimsFactory = new();
        claimsFactory.Setup(value => value.CreateAsync(It.IsAny<User>())).ReturnsAsync(new ClaimsPrincipal(new ClaimsIdentity()));
        Mock<IAuthenticationSchemeProvider> schemes = new();
        Mock<IUserConfirmation<User>> confirmation = new();
        confirmation.Setup(value => value.IsConfirmedAsync(It.IsAny<UserManager<User>>(), It.IsAny<User>())).ReturnsAsync(true);
        Mock<IAuthenticationService> authentication = new();
        authentication.Setup(value => value.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string?>()))
            .ReturnsAsync(AuthenticateResult.NoResult());
        ServiceProvider requestServices = new ServiceCollection()
            .AddSingleton(authentication.Object)
            .BuildServiceProvider();
        DefaultHttpContext httpContext = new() { RequestServices = requestServices };
        AuditingSignInManager manager = new(
            userManager,
            new HttpContextAccessor { HttpContext = httpContext },
            claimsFactory.Object,
            Options.Create(new IdentityOptions()),
            NullLogger<AuditingSignInManager>.Instance,
            schemes.Object,
            confirmation.Object,
            writer,
            requestContext.Object);
        manager.AuthenticationScheme = IdentityConstants.ApplicationScheme;
        return manager;
    }

    private static Mock<ISecurityAuditWriter> AuditWriter()
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAuditWriteResult.Recorded);
        return writer;
    }

    private static TestUsersDbContext CreateDbContext()
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        return new TestUsersDbContext(options);
    }

    private sealed class TestUsersDbContext(DbContextOptions<UsersDbContext> options) : UsersDbContext(options);

    private sealed class TestTwoFactorTokenProvider : IUserTwoFactorTokenProvider<User>
    {
        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<User> manager, User user) => Task.FromResult(true);

        public Task<string> GenerateAsync(string purpose, UserManager<User> manager, User user) => Task.FromResult(string.Empty);

        public Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager, User user) => Task.FromResult(false);
    }
}
