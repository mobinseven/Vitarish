using System.Security.Claims;
using FluentAssertions;
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
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Security;

public sealed class ChangeUserAccountStatusCommandHandlerTests
{
    private const string ManagedUserId = "managed-user";
    private const string SeedRevision = "seed-revision";

    [Fact]
    public async Task Disabling_a_signed_out_user_persists_the_status_and_audits_the_administrator()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        Mock<IAuthenticationActiveSessionQuery> activeSessions = ActiveSessions(false);
        ChangeUserAccountStatusCommandHandler handler = CreateHandler(dbContext, auditWriter, activeSessions);

        IdentityResult result = await handler.Handle(
            new ChangeUserAccountStatusCommand(ManagedUserId, Enabled: false),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.IsEnabled.Should().BeFalse();
        persisted.ConcurrencyStamp.Should().NotBe(SeedRevision);
        SecurityAuditEvent disabled = AuditEvents(auditWriter).Single();
        disabled.EventName.Should().Be(SecurityAuditEventCatalog.UserAccountDisabledEvent);
        disabled.Operation.Should().Be(SecurityAuditEventCatalog.UserAccountDisableOperation);
        disabled.Outcome.Should().Be(SecurityAuditOutcome.Succeeded);
        disabled.ActorId.Should().Be("platform-admin");
        disabled.SubjectId.Should().Be(ManagedUserId);
        disabled.RedactedReason.Should().BeNull();
        disabled.Changes.Should().Equal(new SecurityAuditChange(SecurityAuditChangeFields.AccountStatus, SecurityAuditChangeFields.AccountStatusEnabled, SecurityAuditChangeFields.AccountStatusDisabled));
        // Checked before the save and again after it, to catch a sign-in that raced the disable.
        activeSessions.Verify(query => query.HasActiveSessionAsync(ManagedUserId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Disabling_a_signed_in_user_is_rejected_with_a_typed_conflict_and_changes_nothing()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        ChangeUserAccountStatusCommandHandler handler = CreateHandler(dbContext, auditWriter, ActiveSessions(true));

        IdentityResult result = await handler.Handle(
            new ChangeUserAccountStatusCommand(ManagedUserId, Enabled: false),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.Code == UsersCommandErrorCodes.ActiveSessionConflict &&
            error.Description == UsersCommandErrorCodes.ActiveSessionConflictDescription);
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.IsEnabled.Should().BeTrue();
        persisted.ConcurrencyStamp.Should().Be(SeedRevision);
        SecurityAuditEvent denied = AuditEvents(auditWriter).Single();
        denied.EventName.Should().Be(SecurityAuditEventCatalog.UserAccountDisabledEvent);
        denied.Outcome.Should().Be(SecurityAuditOutcome.Denied);
        denied.ActorId.Should().Be("platform-admin");
        denied.SubjectId.Should().Be(ManagedUserId);
        denied.RedactedReason.Should().Be(SecurityAuditReasonCatalog.ActiveSessionConflict);
    }

    [Fact]
    public async Task A_sign_in_that_lands_between_the_session_check_and_the_save_undoes_the_disable()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        Mock<IAuthenticationActiveSessionQuery> activeSessions = new();
        activeSessions.SetupSequence(query => query.HasActiveSessionAsync(ManagedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        ChangeUserAccountStatusCommandHandler handler = CreateHandler(dbContext, auditWriter, activeSessions, isPlatformAdministrator: true);

        IdentityResult result = await handler.Handle(new ChangeUserAccountStatusCommand(ManagedUserId, Enabled: false), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.Code == UsersCommandErrorCodes.ActiveSessionConflict);
        (await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId)).IsEnabled.Should().BeTrue();
        AuditEvents(auditWriter).Should().ContainSingle(audit =>
            audit.Outcome == SecurityAuditOutcome.Denied && audit.RedactedReason == SecurityAuditReasonCatalog.ActiveSessionConflict);
    }

    [Fact]
    public async Task A_failed_restore_records_the_actual_disabled_state()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        Mock<IAuthenticationActiveSessionQuery> activeSessions = new();
        activeSessions.SetupSequence(query => query.HasActiveSessionAsync(ManagedUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        ChangeUserAccountStatusCommandHandler handler = new(
            CreateRestoreFailingUserManager(dbContext),
            activeSessions.Object,
            RequestContext(isPlatformAdministrator: true),
            new UsersManagementAudit(auditWriter.Object, RequestContext(isPlatformAdministrator: true), TimeProvider.System),
            CreateCache());

        Func<Task> handle = () => handler.Handle(new ChangeUserAccountStatusCommand(ManagedUserId, Enabled: false), CancellationToken.None);

        await handle.Should().ThrowAsync<InvalidOperationException>();
        (await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId)).IsEnabled.Should().BeFalse();
        SecurityAuditEvent failed = AuditEvents(auditWriter).Single();
        failed.EventName.Should().Be(SecurityAuditEventCatalog.UserAccountDisabledEvent);
        failed.Outcome.Should().Be(SecurityAuditOutcome.Failed);
        failed.Changes.Should().Equal(new SecurityAuditChange(
            SecurityAuditChangeFields.AccountStatus,
            SecurityAuditChangeFields.AccountStatusEnabled,
            SecurityAuditChangeFields.AccountStatusDisabled));
    }

    [Fact]
    public async Task Enabling_a_user_has_no_session_precondition_and_audits_the_administrator()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync(user => user.IsEnabled = false);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        Mock<IAuthenticationActiveSessionQuery> activeSessions = ActiveSessions(true);
        ChangeUserAccountStatusCommandHandler handler = CreateHandler(dbContext, auditWriter, activeSessions);

        IdentityResult result = await handler.Handle(
            new ChangeUserAccountStatusCommand(ManagedUserId, Enabled: true),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.IsEnabled.Should().BeTrue();
        activeSessions.Verify(query => query.HasActiveSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        SecurityAuditEvent enabled = AuditEvents(auditWriter).Single();
        enabled.EventName.Should().Be(SecurityAuditEventCatalog.UserAccountEnabledEvent);
        enabled.Operation.Should().Be(SecurityAuditEventCatalog.UserAccountEnableOperation);
        enabled.Outcome.Should().Be(SecurityAuditOutcome.Succeeded);
        enabled.ActorId.Should().Be("platform-admin");
        enabled.SubjectId.Should().Be(ManagedUserId);
    }

    [Fact]
    public async Task Repeating_the_current_status_is_idempotent_and_audits_success_without_mutating_or_checking_sessions()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync(user => user.IsEnabled = false);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        Mock<IAuthenticationActiveSessionQuery> activeSessions = ActiveSessions(true);
        ChangeUserAccountStatusCommandHandler handler = CreateHandler(dbContext, auditWriter, activeSessions);

        IdentityResult result = await handler.Handle(
            new ChangeUserAccountStatusCommand(ManagedUserId, Enabled: false),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.IsEnabled.Should().BeFalse();
        persisted.ConcurrencyStamp.Should().Be(SeedRevision);
        activeSessions.Verify(query => query.HasActiveSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        SecurityAuditEvent replayed = AuditEvents(auditWriter).Single();
        replayed.EventName.Should().Be(SecurityAuditEventCatalog.UserAccountDisabledEvent);
        replayed.Outcome.Should().Be(SecurityAuditOutcome.Succeeded);
        replayed.Changes.Should().BeNull("repeating the current status changes nothing");
    }

    [Fact]
    public async Task A_non_administrator_disable_is_denied_before_any_user_read_and_is_audited()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        ChangeUserAccountStatusCommandHandler handler = CreateHandler(
            dbContext,
            auditWriter,
            ActiveSessions(false),
            isPlatformAdministrator: false);

        IdentityResult result = await handler.Handle(
            new ChangeUserAccountStatusCommand(ManagedUserId, Enabled: false),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(UsersCommandErrorCodes.Unauthorized);
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.IsEnabled.Should().BeTrue();
        SecurityAuditEvent denied = AuditEvents(auditWriter).Single();
        denied.EventName.Should().Be(SecurityAuditEventCatalog.UserAccountDisabledEvent);
        denied.Outcome.Should().Be(SecurityAuditOutcome.Denied);
        denied.RedactedReason.Should().Be(SecurityAuditReasonCatalog.Unauthorized);
    }

    [Fact]
    public async Task Disabling_an_unknown_user_is_reported_as_not_found_and_is_audited()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        ChangeUserAccountStatusCommandHandler handler = CreateHandler(dbContext, auditWriter, ActiveSessions(false));

        IdentityResult result = await handler.Handle(
            new ChangeUserAccountStatusCommand("missing-user", Enabled: false),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be(UsersCommandErrorCodes.UserNotFound);
        SecurityAuditEvent denied = AuditEvents(auditWriter).Single();
        denied.EventName.Should().Be(SecurityAuditEventCatalog.UserAccountDisabledEvent);
        denied.Outcome.Should().Be(SecurityAuditOutcome.Denied);
        denied.SubjectId.Should().Be("missing-user");
        denied.RedactedReason.Should().Be(SecurityAuditReasonCatalog.UserNotFound);
    }

    [Fact]
    public async Task Newly_persisted_users_are_enabled_by_default()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        dbContext.Users.Add(new User
        {
            Id = "default-state-user",
            UserName = "default-state-user",
            SecurityStamp = "security-stamp",
            ConcurrencyStamp = "revision"
        });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == "default-state-user");

        persisted.IsEnabled.Should().BeTrue();
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

    private static Mock<IAuthenticationActiveSessionQuery> ActiveSessions(bool hasActiveSession)
    {
        Mock<IAuthenticationActiveSessionQuery> query = new();
        query.Setup(value => value.HasActiveSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasActiveSession);
        return query;
    }

    private static ChangeUserAccountStatusCommandHandler CreateHandler(
        UsersDbContext dbContext,
        Mock<ISecurityAuditWriter> auditWriter,
        Mock<IAuthenticationActiveSessionQuery> activeSessions,
        bool isPlatformAdministrator = true) => new(
        CreateUserManager(dbContext),
        activeSessions.Object,
        RequestContext(isPlatformAdministrator),
        new UsersManagementAudit(auditWriter.Object, RequestContext(isPlatformAdministrator), TimeProvider.System),
        CreateCache());

    private static IRequestContext RequestContext(bool isPlatformAdministrator)
    {
        Mock<IRequestContext> context = new();
        ClaimsIdentity identity = isPlatformAdministrator
            ? new ClaimsIdentity([new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)], "test")
            : new ClaimsIdentity([], "test");
        context.SetupGet(value => value.User).Returns(new ClaimsPrincipal(identity));
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

    private static UserManager<User> CreateRestoreFailingUserManager(UsersDbContext dbContext) => new RestoreFailingUserManager(
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

    private static async Task<TestUsersDbContext> CreateDbContextWithManagedUserAsync(Action<User>? configure = null)
    {
        TestUsersDbContext dbContext = CreateDbContext();
        User managedUser = new()
        {
            Id = ManagedUserId,
            UserName = "managed@example.test",
            NormalizedUserName = "MANAGED@EXAMPLE.TEST",
            Email = "managed@example.test",
            NormalizedEmail = "MANAGED@EXAMPLE.TEST",
            FirstName = "Managed",
            LastName = "User",
            SecurityStamp = "security-stamp",
            ConcurrencyStamp = SeedRevision
        };
        configure?.Invoke(managedUser);
        dbContext.Users.Add(managedUser);
        await dbContext.SaveChangesAsync();
        return dbContext;
    }

    private sealed class TestUsersDbContext(DbContextOptions<UsersDbContext> options) : UsersDbContext(options);

    private sealed class RestoreFailingUserManager(
        IUserStore<User> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<User> passwordHasher,
        IEnumerable<IUserValidator<User>> userValidators,
        IEnumerable<IPasswordValidator<User>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<User>> logger)
        : UserManager<User>(
            store,
            optionsAccessor,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger)
    {
        private int _updateAttempts;

        public override Task<IdentityResult> UpdateAsync(User user)
        {
            _updateAttempts++;
            return _updateAttempts == 1
                ? base.UpdateAsync(user)
                : Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "RestoreFailure", Description = "restore failed" }));
        }
    }
}
