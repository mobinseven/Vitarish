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
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Data;

public sealed class ContactChangeHandlerTests
{
    private const string Revision = "rev-under-test";

    [DockerFact]
    public async Task Set_email_resolves_only_the_new_email_and_username()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using (UsersDbContext seed = PostgresTestSupport.CreateContext(connectionString))
        {
            seed.Users.Add(EmailAccount());
            await seed.SaveChangesAsync();
        }

        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        SetUserEmailCommandHandler handler = new(
            CreateUserManager(PostgresTestSupport.CreateContext(connectionString)),
            CreateSender(PostgresTestSupport.CreateContext(connectionString)),
            CreateCache(),
            CreateAudit(auditWriter));

        IdentityResult result = await handler.Handle(new SetUserEmailCommand("under-test", "new@example.test"), CancellationToken.None);

        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors.Select(error => $"{error.Code}:{error.Description}")));
        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        (await verification.Users.AnyAsync(user => user.NormalizedEmail == "OLD@EXAMPLE.TEST")).Should().BeFalse();
        (await verification.Users.AnyAsync(user => user.NormalizedEmail == "NEW@EXAMPLE.TEST")).Should().BeTrue();
        (await verification.Users.AnyAsync(user => user.NormalizedUserName == "OLD@EXAMPLE.TEST")).Should().BeFalse();
        (await verification.Users.AnyAsync(user => user.NormalizedUserName == "NEW@EXAMPLE.TEST")).Should().BeTrue();

        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditEvent &&
            entry.Outcome == SecurityAuditOutcome.Succeeded);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent &&
            entry.SubjectId == "under-test" &&
            entry.Outcome == SecurityAuditOutcome.Succeeded &&
            entry.RedactedReason == SecurityAuditReasonCatalog.UserContactIdentifierChanged);
    }

    [DockerFact]
    public async Task Set_email_rejects_a_taken_identifier_and_keeps_the_row()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using (UsersDbContext seed = PostgresTestSupport.CreateContext(connectionString))
        {
            seed.Users.Add(EmailAccount());
            seed.Users.Add(ContactAccount("other-username-owner", "username-owned@example.test", null, null));
            seed.Users.Add(ContactAccount("other-email-owner", "other-email-owner", "email-owned@example.test", null));
            await seed.SaveChangesAsync();
        }

        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        SetUserEmailCommandHandler handler = new(
            CreateUserManager(PostgresTestSupport.CreateContext(connectionString)),
            CreateSender(PostgresTestSupport.CreateContext(connectionString)),
            CreateCache(),
            CreateAudit(auditWriter));

        IdentityResult usernameOwned = await handler.Handle(new SetUserEmailCommand("under-test", "username-owned@example.test"), CancellationToken.None);
        IdentityResult emailOwned = await handler.Handle(new SetUserEmailCommand("under-test", "email-owned@example.test"), CancellationToken.None);

        usernameOwned.Succeeded.Should().BeFalse();
        usernameOwned.Errors.Should().ContainSingle(error => error.Code == nameof(UsersCommandFailureCode.ContactIdentifierInUse));
        emailOwned.Succeeded.Should().BeFalse();
        emailOwned.Errors.Should().ContainSingle(error => error.Code == nameof(UsersCommandFailureCode.ContactIdentifierInUse));

        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        User row = await verification.Users.SingleAsync(user => user.Id == "under-test");
        row.Email.Should().Be("old@example.test");
        row.UserName.Should().Be("old@example.test");
        row.ConcurrencyStamp.Should().Be(Revision);

        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Count(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditFailureEvent &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.MutationRejected).Should().Be(2);
        events.Should().Contain(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent &&
            entry.SubjectId == "under-test" &&
            entry.Outcome == SecurityAuditOutcome.Denied);
    }

    [DockerFact]
    public async Task Set_email_twice_keeps_identifiers_unchanged()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using (UsersDbContext seed = PostgresTestSupport.CreateContext(connectionString))
        {
            seed.Users.Add(EmailAccount());
            await seed.SaveChangesAsync();
        }

        SetUserEmailCommandHandler handler = new(
            CreateUserManager(PostgresTestSupport.CreateContext(connectionString)),
            CreateSender(PostgresTestSupport.CreateContext(connectionString)),
            CreateCache(),
            CreateAudit(AuditWriter()));

        IdentityResult first = await handler.Handle(new SetUserEmailCommand("under-test", "new@example.test"), CancellationToken.None);
        IdentityResult second = await handler.Handle(new SetUserEmailCommand("under-test", "new@example.test"), CancellationToken.None);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        User row = await verification.Users.SingleAsync(user => user.Id == "under-test");
        row.Email.Should().Be("new@example.test");
        row.UserName.Should().Be("new@example.test");
        row.NormalizedUserName.Should().Be("NEW@EXAMPLE.TEST");
    }

    [DockerFact]
    public async Task Set_phone_resolves_only_the_new_phone_and_username()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using (UsersDbContext seed = PostgresTestSupport.CreateContext(connectionString))
        {
            seed.Users.Add(PhoneAccount());
            await seed.SaveChangesAsync();
        }

        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        SetUserPhoneNumberCommandHandler handler = new(
            CreateUserManager(PostgresTestSupport.CreateContext(connectionString)),
            CreateSender(PostgresTestSupport.CreateContext(connectionString)),
            CreateCache(),
            CreateAudit(auditWriter));

        IdentityResult result = await handler.Handle(new SetUserPhoneNumberCommand("under-test", "+989129999999"), CancellationToken.None);

        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors.Select(error => $"{error.Code}:{error.Description}")));
        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        (await verification.Users.AnyAsync(user => user.PhoneNumber == "+989121234567")).Should().BeFalse();
        (await verification.Users.AnyAsync(user => user.PhoneNumber == "+989129999999")).Should().BeTrue();
        (await verification.Users.AnyAsync(user => user.NormalizedUserName == "+989121234567")).Should().BeFalse();
        (await verification.Users.AnyAsync(user => user.NormalizedUserName == "+989129999999")).Should().BeTrue();

        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditEvent &&
            entry.Outcome == SecurityAuditOutcome.Succeeded);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent &&
            entry.SubjectId == "under-test" &&
            entry.Outcome == SecurityAuditOutcome.Succeeded &&
            entry.RedactedReason == SecurityAuditReasonCatalog.UserContactIdentifierChanged);
    }

    [DockerFact]
    public async Task Set_phone_rejects_a_taken_identifier_and_keeps_the_row()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using (UsersDbContext seed = PostgresTestSupport.CreateContext(connectionString))
        {
            seed.Users.Add(PhoneAccount());
            seed.Users.Add(ContactAccount("other-phone-owner", null, null, "+989125555555"));
            seed.Users.Add(ContactAccount("other-phone-username-owner", "+989127777777", null, null));
            await seed.SaveChangesAsync();
        }

        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        SetUserPhoneNumberCommandHandler handler = new(
            CreateUserManager(PostgresTestSupport.CreateContext(connectionString)),
            CreateSender(PostgresTestSupport.CreateContext(connectionString)),
            CreateCache(),
            CreateAudit(auditWriter));

        IdentityResult phoneOwned = await handler.Handle(new SetUserPhoneNumberCommand("under-test", "+989125555555"), CancellationToken.None);
        IdentityResult usernameOwned = await handler.Handle(new SetUserPhoneNumberCommand("under-test", "+989127777777"), CancellationToken.None);

        phoneOwned.Succeeded.Should().BeFalse();
        phoneOwned.Errors.Should().ContainSingle(error => error.Code == nameof(UsersCommandFailureCode.ContactIdentifierInUse));
        usernameOwned.Succeeded.Should().BeFalse();
        usernameOwned.Errors.Should().ContainSingle(error => error.Code == nameof(UsersCommandFailureCode.ContactIdentifierInUse));

        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        User row = await verification.Users.SingleAsync(user => user.Id == "under-test");
        row.PhoneNumber.Should().Be("+989121234567");
        row.UserName.Should().Be("+989121234567");
        row.ConcurrencyStamp.Should().Be(Revision);
    }

    [DockerFact]
    public async Task Set_phone_twice_keeps_identifiers_unchanged()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using (UsersDbContext seed = PostgresTestSupport.CreateContext(connectionString))
        {
            seed.Users.Add(PhoneAccount());
            await seed.SaveChangesAsync();
        }

        SetUserPhoneNumberCommandHandler handler = new(
            CreateUserManager(PostgresTestSupport.CreateContext(connectionString)),
            CreateSender(PostgresTestSupport.CreateContext(connectionString)),
            CreateCache(),
            CreateAudit(AuditWriter()));

        IdentityResult first = await handler.Handle(new SetUserPhoneNumberCommand("under-test", "+989129999999"), CancellationToken.None);
        IdentityResult second = await handler.Handle(new SetUserPhoneNumberCommand("under-test", "+989129999999"), CancellationToken.None);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        User row = await verification.Users.SingleAsync(user => user.Id == "under-test");
        row.PhoneNumber.Should().Be("+989129999999");
        row.UserName.Should().Be("+989129999999");
        row.NormalizedUserName.Should().Be("+989129999999");
    }

    [DockerFact]
    public async Task Update_managed_email_change_resolves_only_the_new_identifiers()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using (UsersDbContext seed = PostgresTestSupport.CreateContext(connectionString))
        {
            seed.Users.Add(EmailAccount());
            await seed.SaveChangesAsync();
        }

        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = new(
            CreateUserManager(PostgresTestSupport.CreateContext(connectionString)),
            CreateSender(PostgresTestSupport.CreateContext(connectionString)),
            CreateCache(),
            CreateAudit(auditWriter),
            RequestContext());

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand("under-test", "new@example.test", null, null, null, Revision),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors.Select(error => $"{error.Code}:{error.Description}")));
        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        User row = await verification.Users.SingleAsync(user => user.Id == "under-test");
        row.NormalizedEmail.Should().Be("NEW@EXAMPLE.TEST");
        row.NormalizedUserName.Should().Be("NEW@EXAMPLE.TEST");
        row.Email.Should().Be("new@example.test");
        row.UserName.Should().Be("new@example.test");

        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditEvent &&
            entry.Outcome == SecurityAuditOutcome.Succeeded);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent &&
            entry.SubjectId == "under-test" &&
            entry.Outcome == SecurityAuditOutcome.Succeeded &&
            entry.RedactedReason == UsersSecurityAuditEvents.UserContactIdentifierChangedReason);
    }

    [DockerFact]
    public async Task Update_managed_rejects_a_taken_identifier_and_keeps_the_row()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using (UsersDbContext seed = PostgresTestSupport.CreateContext(connectionString))
        {
            seed.Users.Add(EmailAccount());
            seed.Users.Add(ContactAccount("other-username-owner", "username-owned@example.test", null, null));
            seed.Users.Add(ContactAccount("other-email-owner", "other-email-owner", "email-owned@example.test", null));
            await seed.SaveChangesAsync();
        }

        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = new(
            CreateUserManager(PostgresTestSupport.CreateContext(connectionString)),
            CreateSender(PostgresTestSupport.CreateContext(connectionString)),
            CreateCache(),
            CreateAudit(auditWriter),
            RequestContext());

        IdentityResult usernameOwned = await handler.Handle(
            new UpdateManagedUserCommand("under-test", "username-owned@example.test", null, null, null, Revision),
            CancellationToken.None);
        IdentityResult emailOwned = await handler.Handle(
            new UpdateManagedUserCommand("under-test", "email-owned@example.test", null, null, null, Revision),
            CancellationToken.None);

        usernameOwned.Succeeded.Should().BeFalse();
        usernameOwned.Errors.Should().ContainSingle(error => error.Code == nameof(UsersCommandFailureCode.ContactIdentifierInUse));
        emailOwned.Succeeded.Should().BeFalse();
        emailOwned.Errors.Should().ContainSingle(error => error.Code == nameof(UsersCommandFailureCode.ContactIdentifierInUse));

        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        User row = await verification.Users.SingleAsync(user => user.Id == "under-test");
        row.Email.Should().Be("old@example.test");
        row.UserName.Should().Be("old@example.test");
        row.ConcurrencyStamp.Should().Be(Revision);

        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Count(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditFailureEvent &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.MutationRejected).Should().Be(2);
    }

    [DockerFact]
    public async Task Update_managed_same_call_twice_keeps_identifiers_unchanged()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using (UsersDbContext seed = PostgresTestSupport.CreateContext(connectionString))
        {
            seed.Users.Add(EmailAccount());
            await seed.SaveChangesAsync();
        }

        UpdateManagedUserCommandHandler handler = new(
            CreateUserManager(PostgresTestSupport.CreateContext(connectionString)),
            CreateSender(PostgresTestSupport.CreateContext(connectionString)),
            CreateCache(),
            CreateAudit(AuditWriter()),
            RequestContext());

        IdentityResult first = await handler.Handle(
            new UpdateManagedUserCommand("under-test", "new@example.test", null, null, null, Revision),
            CancellationToken.None);
        string refreshedRevision = await CurrentRevisionAsync(connectionString, "under-test");
        IdentityResult second = await handler.Handle(
            new UpdateManagedUserCommand("under-test", "new@example.test", null, null, null, refreshedRevision),
            CancellationToken.None);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        User row = await verification.Users.SingleAsync(user => user.Id == "under-test");
        row.Email.Should().Be("new@example.test");
        row.UserName.Should().Be("new@example.test");
        row.NormalizedUserName.Should().Be("NEW@EXAMPLE.TEST");
    }

    [DockerFact]
    public async Task Update_managed_wrong_revision_returns_concurrency_failure()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using (UsersDbContext seed = PostgresTestSupport.CreateContext(connectionString))
        {
            seed.Users.Add(EmailAccount());
            await seed.SaveChangesAsync();
        }

        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = new(
            CreateUserManager(PostgresTestSupport.CreateContext(connectionString)),
            CreateSender(PostgresTestSupport.CreateContext(connectionString)),
            CreateCache(),
            CreateAudit(auditWriter),
            RequestContext());

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand("under-test", "new@example.test", null, null, null, "stale-revision"),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.Code == nameof(UsersCommandFailureCode.ConcurrencyFailure));
        await using UsersDbContext verification = PostgresTestSupport.CreateContext(connectionString);
        User row = await verification.Users.SingleAsync(user => user.Id == "under-test");
        row.Email.Should().Be("old@example.test");
        row.UserName.Should().Be("old@example.test");
        row.ConcurrencyStamp.Should().Be(Revision);

        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Should().Contain(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditFailureEvent &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.SecurityPolicyConcurrencyConflict);
    }

    [DockerFact]
    public async Task UserNameIndex_violation_is_classified_as_a_contact_identifier_conflict()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connectionString = await PostgresTestSupport.CreateDatabaseAsync(container);
        await PostgresTestSupport.ApplyAllMigrationsAsync(connectionString);
        await using (UsersDbContext seed = PostgresTestSupport.CreateContext(connectionString))
        {
            seed.Users.Add(ContactAccount("first", "dup@example.test", null, null));
            await seed.SaveChangesAsync();
        }

        await using UsersDbContext racer = PostgresTestSupport.CreateContext(connectionString);
        racer.Users.Add(ContactAccount("second", "DUP@example.test", null, null));
        DbUpdateException violation = await RecordUniqueViolationAsync(() => racer.SaveChangesAsync())
            ?? throw new InvalidOperationException("A duplicate user name was expected to violate the unique index.");

        ContactIdentifierUniqueViolation.Matches(violation).Should().BeTrue();
    }

    private static async Task<string> CurrentRevisionAsync(string connectionString, string userId)
    {
        await using UsersDbContext context = PostgresTestSupport.CreateContext(connectionString);
        return (await context.Users.SingleAsync(user => user.Id == userId)).ConcurrencyStamp!;
    }

    private static async Task<DbUpdateException> RecordUniqueViolationAsync(Func<Task> saveChanges) =>
        (DbUpdateException)(await Record.ExceptionAsync(saveChanges)
            ?? throw new InvalidOperationException("A unique-constraint violation was expected."));

    private static User EmailAccount() => new()
    {
        Id = "under-test",
        UserName = "old@example.test",
        NormalizedUserName = "OLD@EXAMPLE.TEST",
        Email = "old@example.test",
        NormalizedEmail = "OLD@EXAMPLE.TEST",
        FirstName = "First",
        LastName = "Last",
        SecurityStamp = "security-under-test",
        ConcurrencyStamp = Revision
    };

    private static User PhoneAccount() => new()
    {
        Id = "under-test",
        UserName = "+989121234567",
        NormalizedUserName = "+989121234567",
        PhoneNumber = "+989121234567",
        FirstName = "First",
        LastName = "Last",
        SecurityStamp = "security-under-test",
        ConcurrencyStamp = Revision
    };

    private static User ContactAccount(string id, string? userName, string? email, string? phoneNumber) => new()
    {
        Id = id,
        UserName = userName ?? email ?? phoneNumber!,
        NormalizedUserName = (userName ?? email ?? phoneNumber!)!.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email?.ToUpperInvariant(),
        PhoneNumber = phoneNumber,
        SecurityStamp = "security-" + id,
        ConcurrencyStamp = "rev-" + id
    };

    private static Mock<ISecurityAuditWriter> AuditWriter()
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        return writer;
    }

    private static SecurityAuditEvent[] AuditEvents(Mock<ISecurityAuditWriter> auditWriter) => auditWriter.Invocations
        .Select(invocation => (SecurityAuditEvent)invocation.Arguments[0])
        .ToArray();

    private static UsersManagementAudit CreateAudit(Mock<ISecurityAuditWriter> auditWriter) =>
        new(auditWriter.Object, RequestContext(), TimeProvider.System);

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

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(
        Options.Create(new MemoryDistributedCacheOptions()));

    private static ISender CreateSender(UsersDbContext dbContext)
    {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(CheckUserEmailInUseQueryHandler).Assembly));
        _ = services.AddSingleton<UsersDbContext>(dbContext);
        _ = services.AddSingleton<ILookupNormalizer>(new UpperInvariantLookupNormalizer());
        return services.BuildServiceProvider().GetRequiredService<ISender>();
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
