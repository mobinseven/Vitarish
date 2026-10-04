using System.Security.Claims;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
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
using Testcontainers.PostgreSql;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Data;

public sealed class PhoneLoginRealStoreTests
{
    [DockerFact]
    public async Task Malformed_number_creates_no_account_in_the_real_store()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);

        (IAuthenticationUserDirectory directory, BeginPhoneLoginHandler handler) = CreatePhoneLogin(connectionString);

        AuthenticationResult<ChallengeResult> result = await handler.Handle(
            new BeginPhoneLoginCommand("abc", null), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidInput);
        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        (await verification.Users.CountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task Local_and_persian_formats_create_one_account_with_the_e164_number()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);

        (IAuthenticationUserDirectory directory, BeginPhoneLoginHandler handler) = CreatePhoneLogin(connectionString);

        AuthenticationResult<ChallengeResult> local = await handler.Handle(
            new BeginPhoneLoginCommand("09123456789", null), CancellationToken.None);
        AuthenticationResult<ChallengeResult> persian = await handler.Handle(
            new BeginPhoneLoginCommand("۰۹۱۲۳۴۵۶۷۸۹", null), CancellationToken.None);

        local.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
        persian.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();

        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        List<User> accounts = await verification.Users.AsNoTracking().ToListAsync();
        accounts.Should().ContainSingle();
        accounts[0].PhoneNumber.Should().Be("+989123456789");
        accounts[0].UserName.Should().Be("+989123456789");
    }

    private static (IAuthenticationUserDirectory Directory, BeginPhoneLoginHandler Handler) CreatePhoneLogin(string connectionString)
    {
        UsersDbContext usersDb = PostgresTestSupport.CreateContext(connectionString);
        ISender sender = CreateSender(usersDb);
        AspNetIdentityAuthenticationUserDirectory directory = new(
            CreateUserManager(usersDb), sender, new HttpContextAccessor());

        Mock<IAuthenticationCodeGenerator> codeGenerator = new();
        codeGenerator.Setup(value => value.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456");
        Mock<IPhoneAuthenticationCodeSender> phoneSender = new();
        Mock<IAuthenticationAttemptTracker> attemptTracker = new();
        attemptTracker.Setup(value => value.IsDeniedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Mock<IAuthenticationSecurityPolicyProvider> securityPolicy = new();
        securityPolicy.Setup(value => value.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAccessPolicy.Default);

        BeginPhoneLoginHandler handler = new(
            new InMemoryAuthenticationChallengeStore(),
            directory,
            codeGenerator.Object,
            phoneSender.Object,
            AuthenticationChallengePolicy.Default,
            attemptTracker.Object,
            securityPolicy.Object,
            new AuthenticationAttemptKeyFactory(Options.Create(new AuthenticationAttemptKeyOptions
            {
                Realm = "tests",
                CurrentPepper = "test-authentication-attempt-key-pepper-20260829"
            })));
        return (directory, handler);
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
        _ = services.AddSingleton<IRequestContext>(RequestContext());
        _ = services.AddSingleton<ISecurityAuditWriter>(AuditWriter());
        _ = services.AddSingleton(provider => new UsersManagementAudit(
            provider.GetRequiredService<ISecurityAuditWriter>(),
            provider.GetRequiredService<IRequestContext>(),
            TimeProvider.System));
        return services.BuildServiceProvider().GetRequiredService<ISender>();
    }

    private static ISecurityAuditWriter AuditWriter()
    {
        Mock<ISecurityAuditWriter> auditWriter = new();
        auditWriter.Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        return auditWriter.Object;
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
