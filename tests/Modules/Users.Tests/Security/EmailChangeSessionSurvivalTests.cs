using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Security;

public sealed class EmailChangeSessionSurvivalTests
{
    [Fact]
    public async Task Session_still_validates_after_complete_change_email()
    {
        await using UsersDbContext usersDb = CreateUsersDbContext();
        usersDb.Users.Add(new User
        {
            Id = "session-user",
            UserName = "old@example.test",
            NormalizedUserName = "OLD@EXAMPLE.TEST",
            Email = "old@example.test",
            NormalizedEmail = "OLD@EXAMPLE.TEST",
            FirstName = "First",
            LastName = "Last",
            SecurityStamp = "security-session-user",
            ConcurrencyStamp = "rev-session-user"
        });
        await usersDb.SaveChangesAsync();

        AuthenticationSessionRegistry registry = CreateRegistry();
        AuthenticationSessionEstablishmentResult session = await registry.EstablishAsync("session-user", CancellationToken.None);
        session.Succeeded.Should().BeTrue();

        InMemoryAuthenticationChallengeStore store = new();
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreateEmailChangeChallenge(
            "session-user", "new@example.test", DateTimeOffset.UtcNow);
        await store.SaveAsync(challenge, CancellationToken.None);
        Mock<IAuthenticationCodeVerifier> verifier = new();
        verifier.Setup(value => value.VerifyAsync("session-user", challenge.TokenPurpose, "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        UserAccountUpdater updater = new(CreateUserManager(usersDb), CreateSender(usersDb));
        CompleteChangeEmailHandler handler = new(store, verifier.Object, updater);

        AuthenticationResult<bool> result = await handler.Handle(
            new CompleteChangeEmailCommand("session-user", challenge.Id.Value, "123456"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Success>();
        User updated = await usersDb.Users.AsNoTracking().SingleAsync(user => user.Id == "session-user");
        updated.Email.Should().Be("new@example.test");

        AuthenticationSessionValidationResult validation = await registry.ValidateAndTouchAsync(
            session.SessionId!, "session-user", "tenant-1", CancellationToken.None);
        validation.IsValid.Should().BeTrue();
        (await registry.HasActiveSessionAsync("session-user", CancellationToken.None)).Should().BeTrue();
    }

    private static AuthenticationSessionRegistry CreateRegistry()
    {
        DbContextOptions<AuthenticationDbContext> options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        AuthenticationDbContext db = new(options);
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAccessPolicy.Default);
        Mock<IClientIpAddressAccessor> clientIp = new();
        clientIp.SetupGet(accessor => accessor.ClientIpAddress).Returns("127.0.0.1");
        Mock<ISecurityAuditWriter> auditWriter = new();
        auditWriter.Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        return new AuthenticationSessionRegistry(
            db, policyProvider.Object, clientIp.Object, RequestContext(), auditWriter.Object, TimeProvider.System);
    }

    private static UsersDbContext CreateUsersDbContext()
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new UsersDbContext(options);
    }

    private static IRequestContext RequestContext()
    {
        Mock<IRequestContext> context = new();
        context.SetupGet(value => value.TenantId).Returns("tenant-1");
        context.SetupGet(value => value.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)],
            "test")));
        context.SetupGet(value => value.UserId).Returns("platform-admin");
        context.SetupGet(value => value.RequestId).Returns("request-id");
        context.SetupGet(value => value.TraceId).Returns("trace-id");
        return context.Object;
    }

    private static ISender CreateSender(UsersDbContext dbContext)
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(CheckUserEmailInUseQueryHandler).Assembly));
        _ = services.AddSingleton<UsersDbContext>(dbContext);
        _ = services.AddSingleton(CreateUserManager(dbContext));
        _ = services.AddSingleton<ILookupNormalizer>(new UpperInvariantLookupNormalizer());
        _ = services.AddSingleton<IDistributedCache>(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));
        IRequestContext requestContext = RequestContext();
        _ = services.AddSingleton(requestContext);
        _ = services.AddSingleton<ISecurityAuditWriter>(CreateAuditWriter().Object);
        _ = services.AddSingleton(provider => new UsersManagementAudit(
            provider.GetRequiredService<ISecurityAuditWriter>(),
            provider.GetRequiredService<IRequestContext>(),
            TimeProvider.System));
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private static Mock<ISecurityAuditWriter> CreateAuditWriter()
    {
        Mock<ISecurityAuditWriter> auditWriter = new();
        auditWriter.Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        return auditWriter;
    }

    private static UserManager<User> CreateUserManager(UsersDbContext dbContext) => new(
        new UserStore<User>(dbContext),
        Options.Create(new IdentityOptions()),
        new PasswordHasher<User>(),
        [new UserValidator<User>()],
        [new PasswordValidator<User>()],
        new UpperInvariantLookupNormalizer(),
        new IdentityErrorDescriber(),
        new ServiceCollection().BuildServiceProvider(),
        NullLogger<UserManager<User>>.Instance);
}
