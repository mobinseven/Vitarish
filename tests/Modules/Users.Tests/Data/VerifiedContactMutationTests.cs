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
using Testcontainers.PostgreSql;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Tests.Data;

public sealed class VerifiedContactMutationTests
{
    private const string EmailMutationUserId = "email-mutation-user";
    private const string PhoneMutationUserId = "phone-mutation-user";
    private const string VerifiedEmailUserId = "verified-email-user";
    private const string VerifiedPhoneUserId = "verified-phone-user";
    private const string NoOpEmailUserId = "no-op-email-user";
    private const string NoOpPhoneUserId = "no-op-phone-user";
    private const string LastEmailChannelUserId = "last-email-channel-user";
    private const string LastPhoneChannelUserId = "last-phone-channel-user";
    private const string ConcurrencyUserId = "concurrency-user";

    private const string EmailMutationOld = "email-mutation@example.test";
    private const string EmailMutationNew = "email-mutation-updated@example.test";
    private const string PhoneMutationEmail = "phone-mutation@example.test";
    private const string VerifiedEmailOld = "verified-email@example.test";
    private const string VerifiedEmailNew = "verified-email-updated@example.test";
    private const string VerifiedPhoneEmail = "verified-phone@example.test";
    private const string NoOpEmail = "no-op-email@example.test";
    private const string NoOpPhoneEmail = "no-op-phone@example.test";
    private const string LastEmailChannel = "last-email@example.test";
    private const string LastPhoneChannelEmail = "last-phone@example.test";
    private const string ConcurrencyEmail = "concurrency@example.test";
    private const string AttemptedConcurrencyEmail = "attempted-concurrency@example.test";
    private const string CompetingConcurrencyEmail = "competing-concurrency@example.test";

    private const string PhoneMutationOld = "+989121234502";
    private const string PhoneMutationNew = "+989121234503";
    private const string VerifiedEmailPhone = "+989121234504";
    private const string VerifiedPhoneOld = "+989121234505";
    private const string VerifiedPhoneNew = "+989121234506";
    private const string NoOpEmailPhone = "+989121234507";
    private const string NoOpPhone = "+989121234508";
    private const string LastEmailChannelPhone = "+989121234509";
    private const string LastPhoneChannel = "+989121234510";
    private const string ConcurrencyPhone = "+989121234511";

    [Fact]
    public async Task Unverified_contact_mutations_clear_only_the_changed_confirmation_when_another_channel_remains()
    {
        await using UsersDbContext dbContext = CreateInMemoryContext();
        dbContext.Users.AddRange(
            Account(EmailMutationUserId, EmailMutationOld, emailConfirmed: true, "+989121234501", phoneConfirmed: true),
            Account(PhoneMutationUserId, PhoneMutationEmail, emailConfirmed: true, PhoneMutationOld, phoneConfirmed: true));
        await dbContext.SaveChangesAsync();

        using ServiceProvider services = CreateServices(dbContext, CreateUserManager(dbContext), AuditWriter());
        ISender sender = services.GetRequiredService<ISender>();

        IdentityResult emailResult = await sender.Send(new SetUserEmailCommand(EmailMutationUserId, EmailMutationNew));
        IdentityResult phoneResult = await sender.Send(new SetUserPhoneNumberCommand(PhoneMutationUserId, PhoneMutationNew));

        emailResult.Succeeded.Should().BeTrue(string.Join("; ", emailResult.Errors.Select(error => $"{error.Code}:{error.Description}")));
        phoneResult.Succeeded.Should().BeTrue(string.Join("; ", phoneResult.Errors.Select(error => $"{error.Code}:{error.Description}")));

        User emailUser = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == EmailMutationUserId);
        emailUser.Email.Should().Be(EmailMutationNew);
        emailUser.EmailConfirmed.Should().BeFalse();
        emailUser.PhoneNumberConfirmed.Should().BeTrue();
        emailUser.TwoFactorEnabled.Should().BeTrue();

        User phoneUser = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == PhoneMutationUserId);
        phoneUser.PhoneNumber.Should().Be(PhoneMutationNew);
        phoneUser.PhoneNumberConfirmed.Should().BeFalse();
        phoneUser.EmailConfirmed.Should().BeTrue();
        phoneUser.TwoFactorEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Verified_contact_mutations_persist_confirmed_destinations_without_disabling_two_step_sign_in()
    {
        await using UsersDbContext dbContext = CreateInMemoryContext();
        dbContext.Users.AddRange(
            Account(VerifiedEmailUserId, VerifiedEmailOld, emailConfirmed: true, VerifiedEmailPhone, phoneConfirmed: true),
            Account(VerifiedPhoneUserId, VerifiedPhoneEmail, emailConfirmed: true, VerifiedPhoneOld, phoneConfirmed: true));
        await dbContext.SaveChangesAsync();

        using ServiceProvider services = CreateServices(dbContext, CreateUserManager(dbContext), AuditWriter());
        ISender sender = services.GetRequiredService<ISender>();

        IdentityResult emailResult = await sender.Send(new SetVerifiedUserEmailCommand(VerifiedEmailUserId, VerifiedEmailNew));
        IdentityResult phoneResult = await sender.Send(new SetVerifiedUserPhoneNumberCommand(VerifiedPhoneUserId, VerifiedPhoneNew));

        emailResult.Succeeded.Should().BeTrue(string.Join("; ", emailResult.Errors.Select(error => $"{error.Code}:{error.Description}")));
        phoneResult.Succeeded.Should().BeTrue(string.Join("; ", phoneResult.Errors.Select(error => $"{error.Code}:{error.Description}")));

        User emailUser = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == VerifiedEmailUserId);
        emailUser.Email.Should().Be(VerifiedEmailNew);
        emailUser.EmailConfirmed.Should().BeTrue();
        emailUser.PhoneNumberConfirmed.Should().BeTrue();
        emailUser.TwoFactorEnabled.Should().BeTrue();

        User phoneUser = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == VerifiedPhoneUserId);
        phoneUser.PhoneNumber.Should().Be(VerifiedPhoneNew);
        phoneUser.PhoneNumberConfirmed.Should().BeTrue();
        phoneUser.EmailConfirmed.Should().BeTrue();
        phoneUser.TwoFactorEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Unchanged_contact_mutations_preserve_confirmation_and_two_step_state()
    {
        await using UsersDbContext dbContext = CreateInMemoryContext();
        dbContext.Users.AddRange(
            Account(NoOpEmailUserId, NoOpEmail, emailConfirmed: true, NoOpEmailPhone, phoneConfirmed: true),
            Account(NoOpPhoneUserId, NoOpPhoneEmail, emailConfirmed: true, NoOpPhone, phoneConfirmed: true));
        await dbContext.SaveChangesAsync();

        using ServiceProvider services = CreateServices(dbContext, CreateUserManager(dbContext), AuditWriter());
        ISender sender = services.GetRequiredService<ISender>();

        IdentityResult emailResult = await sender.Send(new SetUserEmailCommand(NoOpEmailUserId, NoOpEmail));
        IdentityResult phoneResult = await sender.Send(new SetUserPhoneNumberCommand(NoOpPhoneUserId, NoOpPhone));

        emailResult.Succeeded.Should().BeTrue();
        phoneResult.Succeeded.Should().BeTrue();

        User emailUser = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == NoOpEmailUserId);
        emailUser.EmailConfirmed.Should().BeTrue();
        emailUser.TwoFactorEnabled.Should().BeTrue();

        User phoneUser = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == NoOpPhoneUserId);
        phoneUser.PhoneNumberConfirmed.Should().BeTrue();
        phoneUser.TwoFactorEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Unverified_email_or_phone_change_cannot_remove_the_last_confirmed_channel()
    {
        await using UsersDbContext dbContext = CreateInMemoryContext();
        dbContext.Users.AddRange(
            Account(LastEmailChannelUserId, LastEmailChannel, emailConfirmed: true, LastEmailChannelPhone, phoneConfirmed: false),
            Account(LastPhoneChannelUserId, LastPhoneChannelEmail, emailConfirmed: false, LastPhoneChannel, phoneConfirmed: true));
        await dbContext.SaveChangesAsync();

        using ServiceProvider services = CreateServices(dbContext, CreateUserManager(dbContext), AuditWriter());
        ISender sender = services.GetRequiredService<ISender>();

        IdentityResult emailResult = await sender.Send(new SetUserEmailCommand(LastEmailChannelUserId, "new-last-email@example.test"));
        IdentityResult phoneResult = await sender.Send(new SetUserPhoneNumberCommand(LastPhoneChannelUserId, "+989121234512"));

        string requiredChannelError = nameof(UsersCommandFailureCode.ConfirmedContactRequiredForTwoStepSignIn);
        emailResult.Succeeded.Should().BeFalse();
        emailResult.Errors.Should().Contain(error => error.Code == requiredChannelError);
        phoneResult.Succeeded.Should().BeFalse();
        phoneResult.Errors.Should().Contain(error => error.Code == requiredChannelError);

        User emailUser = await dbContext.Users.SingleAsync(user => user.Id == LastEmailChannelUserId);
        emailUser.Email.Should().Be(LastEmailChannel);
        emailUser.EmailConfirmed.Should().BeTrue();
        emailUser.PhoneNumberConfirmed.Should().BeFalse();
        emailUser.TwoFactorEnabled.Should().BeTrue();

        User phoneUser = await dbContext.Users.SingleAsync(user => user.Id == LastPhoneChannelUserId);
        phoneUser.PhoneNumber.Should().Be(LastPhoneChannel);
        phoneUser.EmailConfirmed.Should().BeFalse();
        phoneUser.PhoneNumberConfirmed.Should().BeTrue();
        phoneUser.TwoFactorEnabled.Should().BeTrue();
    }

    [DockerFact]
    public async Task Email_change_concurrency_conflict_preserves_the_competing_store_value_and_restores_the_scoped_tracker()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        string connectionString = await CreatePostgresDatabaseAsync(container,
            Account(ConcurrencyUserId, ConcurrencyEmail, emailConfirmed: true, ConcurrencyPhone, phoneConfirmed: true));

        await using UsersDbContext handlerContext = PostgresTestSupport.CreateContext(connectionString);
        async Task ApplyCompetingChangeAsync()
        {
            await using UsersDbContext competingContext = PostgresTestSupport.CreateContext(connectionString);
            User competingUser = await competingContext.Users.SingleAsync(user => user.Id == ConcurrencyUserId);
            competingUser.Email = CompetingConcurrencyEmail;
            competingUser.NormalizedEmail = CompetingConcurrencyEmail.ToUpperInvariant();
            competingUser.UserName = CompetingConcurrencyEmail;
            competingUser.NormalizedUserName = CompetingConcurrencyEmail.ToUpperInvariant();
            competingUser.EmailConfirmed = true;
            competingUser.ConcurrencyStamp = "competing-revision";
            await competingContext.SaveChangesAsync();
        }

        RacingUserManager userManager = new(handlerContext, ApplyCompetingChangeAsync);
        using ServiceProvider services = CreateServices(handlerContext, userManager, AuditWriter());
        ISender sender = services.GetRequiredService<ISender>();

        IdentityResult result = await sender.Send(new SetUserEmailCommand(ConcurrencyUserId, AttemptedConcurrencyEmail));

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == new IdentityErrorDescriber().ConcurrencyFailure().Code);

        User? refreshed = await userManager.FindByIdAsync(ConcurrencyUserId);
        refreshed.Should().NotBeNull();
        refreshed!.Email.Should().Be(ConcurrencyEmail);
        refreshed.EmailConfirmed.Should().BeTrue();
        refreshed.TwoFactorEnabled.Should().BeTrue();
        handlerContext.Entry(refreshed).State.Should().Be(EntityState.Unchanged);

        await using UsersDbContext verificationContext = PostgresTestSupport.CreateContext(connectionString);
        User persisted = await verificationContext.Users.AsNoTracking().SingleAsync(user => user.Id == ConcurrencyUserId);
        persisted.Email.Should().Be(CompetingConcurrencyEmail);
        persisted.EmailConfirmed.Should().BeTrue();
        persisted.TwoFactorEnabled.Should().BeTrue();
    }

    private static User Account(
        string id,
        string email,
        bool emailConfirmed,
        string phoneNumber,
        bool phoneConfirmed) => new()
    {
        Id = id,
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        EmailConfirmed = emailConfirmed,
        PhoneNumber = phoneNumber,
        PhoneNumberConfirmed = phoneConfirmed,
        TwoFactorEnabled = true,
        FirstName = "Test",
        LastName = "Account",
        ConcurrencyStamp = "initial-revision-" + id
    };

    private static UsersDbContext CreateInMemoryContext() => new(
        new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static async Task<string> CreatePostgresDatabaseAsync(PostgreSqlContainer container, params User[] users)
    {
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using UsersDbContext seedContext = PostgresTestSupport.CreateContext(connectionString);
        seedContext.Users.AddRange(users);
        await seedContext.SaveChangesAsync();
        return connectionString;
    }

    private static ServiceProvider CreateServices(
        UsersDbContext dbContext,
        UserManager<User> userManager,
        Mock<ISecurityAuditWriter> auditWriter)
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(CheckLoginIdentifierInUseQueryHandler).Assembly));
        _ = services.AddSingleton(dbContext);
        _ = services.AddSingleton<UserManager<User>>(userManager);
        _ = services.AddSingleton<ILookupNormalizer>(new UpperInvariantLookupNormalizer());
        _ = services.AddSingleton<IDistributedCache>(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));
        _ = services.AddSingleton<ISecurityAuditWriter>(auditWriter.Object);
        IRequestContext requestContext = RequestContext();
        _ = services.AddSingleton(requestContext);
        _ = services.AddSingleton(provider => new UsersManagementAudit(
            provider.GetRequiredService<ISecurityAuditWriter>(),
            provider.GetRequiredService<IRequestContext>(),
            TimeProvider.System));
        return services.BuildServiceProvider();
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

    private static Mock<ISecurityAuditWriter> AuditWriter()
    {
        Mock<ISecurityAuditWriter> auditWriter = new();
        auditWriter.Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        return auditWriter;
    }

    private static IRequestContext RequestContext()
    {
        Mock<IRequestContext> context = new();
        context.SetupGet(value => value.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)],
            "test")));
        context.SetupGet(value => value.UserId).Returns("platform-admin");
        context.SetupGet(value => value.RequestId).Returns("request-id");
        context.SetupGet(value => value.TraceId).Returns("trace-id");
        return context.Object;
    }

    private sealed class RacingUserManager : UserManager<User>
    {
        private readonly Func<Task> beforeNextUpdate;
        private bool beforeNextUpdateApplied;

        public RacingUserManager(UsersDbContext dbContext, Func<Task> beforeNextUpdate)
            : base(
                new UserStore<User>(dbContext),
                Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
                new PasswordHasher<User>(),
                [new UserValidator<User>()],
                [new PasswordValidator<User>()],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new ServiceCollection().BuildServiceProvider(),
                NullLogger<UserManager<User>>.Instance)
        {
            this.beforeNextUpdate = beforeNextUpdate;
        }

        public override async Task<IdentityResult> UpdateAsync(User user)
        {
            if (!beforeNextUpdateApplied)
            {
                beforeNextUpdateApplied = true;
                await beforeNextUpdate();
            }

            return await base.UpdateAsync(user);
        }
    }
}
