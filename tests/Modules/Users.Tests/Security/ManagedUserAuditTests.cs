using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
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

namespace Vitarish.Users.Tests.Security;

public sealed class ManagedUserAuditTests
{
    private const string ManagedUserId = "managed-user";

    [Fact]
    public async Task Successful_account_creation_records_the_user_subject_without_contact_data()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UsersManagementAudit managementAudit = new(auditWriter.Object, RequestContext(), TimeProvider.System);
        CreateUserWithEmailCommandHandler handler = new(
            CreateUserManager(dbContext),
            CreateCache(),
            managementAudit);

        User? user = await handler.Handle(new CreateUserWithEmailCommand("created@example.test", "New", "User"), CancellationToken.None);

        user.Should().NotBeNull();
        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Should().ContainSingle(entry => entry.EventName == SecurityAuditEventCatalog.UserCreationEvent);
        SecurityAuditEvent created = events.Single();
        created.Outcome.Should().Be(SecurityAuditOutcome.Succeeded);
        created.SubjectId.Should().Be(user!.Id);
        created.Resource.Should().Be(UsersSecurityAuditEvents.UserResourcePrefix + user.Id);
        created.RedactedReason.Should().BeNull();
        created.Resource.Should().NotContain("created@example.test");
    }

    [Fact]
    public async Task Invalid_account_creation_records_a_failure_without_creating_a_user()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UsersManagementAudit managementAudit = new(auditWriter.Object, RequestContext(), TimeProvider.System);
        CreateUserWithPhoneCommandHandler handler = new(
            CreateUserManager(dbContext),
            CreateCache(),
            managementAudit);

        User? user = await handler.Handle(new CreateUserWithPhoneCommand("09121234567", "New", "User"), CancellationToken.None);

        user.Should().BeNull();
        (await dbContext.Users.CountAsync()).Should().Be(0);
        SecurityAuditEvent failed = AuditEvents(auditWriter).Single();
        failed.EventName.Should().Be(SecurityAuditEventCatalog.UserCreationFailureEvent);
        failed.Outcome.Should().Be(SecurityAuditOutcome.Denied);
        failed.SubjectId.Should().BeNull();
        failed.Resource.Should().Be(UsersSecurityAuditEvents.UnknownUserResource);
        failed.RedactedReason.Should().Be(SecurityAuditReasonCatalog.MutationRejected);
    }

    [Fact]
    public async Task Successful_contact_edit_records_user_edit_and_security_attribute_change()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = new(
            CreateUserManager(dbContext),
            CreateCache(),
            new UsersManagementAudit(auditWriter.Object, RequestContext(), TimeProvider.System),
            RequestContext());

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, "updated@example.test", null, "Changed", "User"),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.Email.Should().Be("updated@example.test");
        persisted.FirstName.Should().Be("Changed");
        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Should().ContainSingle(entry => entry.EventName == SecurityAuditEventCatalog.UserEditEvent && entry.Outcome == SecurityAuditOutcome.Succeeded);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent &&
            entry.SubjectId == ManagedUserId &&
            entry.Outcome == SecurityAuditOutcome.Succeeded &&
            entry.RedactedReason == SecurityAuditReasonCatalog.UserContactIdentifierChanged);
        events.Should().NotContain(entry =>
            entry.RedactedReason != null &&
            entry.RedactedReason.Contains("updated@example.test", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Invalid_contact_edit_is_rejected_and_records_failure_and_denied_attribute_change()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = new(
            CreateUserManager(dbContext),
            CreateCache(),
            new UsersManagementAudit(auditWriter.Object, RequestContext(), TimeProvider.System),
            RequestContext());

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, null, "09121234567", "Changed", "User"),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Single().Code.Should().Be(nameof(UsersCommandFailureCode.InvalidContact));
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.Email.Should().Be("original@example.test");
        persisted.PhoneNumber.Should().BeNull();
        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Should().ContainSingle(entry => entry.EventName == SecurityAuditEventCatalog.UserEditFailureEvent && entry.Outcome == SecurityAuditOutcome.Denied);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent &&
            entry.SubjectId == ManagedUserId &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.MutationRejected);
    }

    private static SecurityAuditEvent[] AuditEvents(Mock<ISecurityAuditWriter> auditWriter) => auditWriter.Invocations
        .Select(invocation => (SecurityAuditEvent)invocation.Arguments[0])
        .ToArray();

    private static Mock<ISecurityAuditWriter> AuditWriter()
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        return writer;
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

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(
        Options.Create(new MemoryDistributedCacheOptions()));

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

    private static TestUsersDbContext CreateDbContext()
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new TestUsersDbContext(options);
    }

    private static async Task<TestUsersDbContext> CreateDbContextWithManagedUserAsync()
    {
        TestUsersDbContext dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Id = ManagedUserId,
            UserName = "original@example.test",
            NormalizedUserName = "ORIGINAL@EXAMPLE.TEST",
            Email = "original@example.test",
            NormalizedEmail = "ORIGINAL@EXAMPLE.TEST",
            SecurityStamp = "security-stamp"
        });
        await dbContext.SaveChangesAsync();
        return dbContext;
    }

    private sealed class TestUsersDbContext(DbContextOptions<UsersDbContext> options) : UsersDbContext(options);
}
