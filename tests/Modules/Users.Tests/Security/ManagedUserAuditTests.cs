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
using MediatR;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Security;

public sealed class ManagedUserAuditTests
{
    private const string ManagedUserId = "managed-user";
    private const string SeedRevision = "seed-revision";

    [Fact]
    public async Task Successful_account_creation_records_the_user_subject_without_contact_data()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UsersManagementAudit managementAudit = new(auditWriter.Object, RequestContext(), TimeProvider.System);
        CreateUserWithEmailCommandHandler handler = new(
            CreateUserManager(dbContext),
            CreateSender(dbContext),
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
    public async Task Setting_an_email_records_the_previous_and_new_value()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        SetUserEmailCommandHandler handler = new(
            CreateUserManager(dbContext),
            CreateSender(dbContext),
            CreateCache(),
            new UsersManagementAudit(auditWriter.Object, RequestContext(), TimeProvider.System));

        IdentityResult result = await handler.Handle(
            new SetUserEmailCommand(ManagedUserId, "updated@example.test"),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        AuditEvents(auditWriter).Single(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditEvent &&
            entry.Outcome == SecurityAuditOutcome.Succeeded).Changes.Should().Equal(
            new SecurityAuditChange(
                SecurityAuditChangeFields.Email,
                "original@example.test",
                "updated@example.test"));
    }

    [Fact]
    public async Task Setting_a_phone_number_records_the_previous_and_new_value()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync(user => user.PhoneNumber = "+14155552671");
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        SetUserPhoneNumberCommandHandler handler = new(
            CreateUserManager(dbContext),
            CreateSender(dbContext),
            CreateCache(),
            new UsersManagementAudit(auditWriter.Object, RequestContext(), TimeProvider.System));

        IdentityResult result = await handler.Handle(
            new SetUserPhoneNumberCommand(ManagedUserId, "+14155552672"),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        AuditEvents(auditWriter).Single(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditEvent &&
            entry.Outcome == SecurityAuditOutcome.Succeeded).Changes.Should().Equal(
            new SecurityAuditChange(
                SecurityAuditChangeFields.PhoneNumber,
                "+14155552671",
                "+14155552672"));
    }

    [Fact]
    public async Task Invalid_account_creation_records_a_failure_without_creating_a_user()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UsersManagementAudit managementAudit = new(auditWriter.Object, RequestContext(), TimeProvider.System);
        CreateUserWithPhoneCommandHandler handler = new(
            CreateUserManager(dbContext),
            CreateSender(dbContext),
            CreateCache(),
            managementAudit);

        User? user = await handler.Handle(new CreateUserWithPhoneCommand("+982112345678", "New", "User"), CancellationToken.None);

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
        UpdateManagedUserCommandHandler handler = CreateUpdateHandler(dbContext, CreateCache(), auditWriter);

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, "updated@example.test", null, "Changed", "User", SeedRevision),
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
        UpdateManagedUserCommandHandler handler = CreateUpdateHandler(dbContext, CreateCache(), auditWriter);

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, null, "+982112345678", "Changed", "User", SeedRevision),
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

    [Fact]
    public async Task Duplicate_email_edit_is_rejected_with_a_typed_conflict_and_no_mutation()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        await SeedConflictingUserAsync(dbContext, "other-user", "taken@example.test", null);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = CreateUpdateHandler(dbContext, CreateCache(), auditWriter);

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, "taken@example.test", null, "Changed", "User", SeedRevision),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(UsersCommandFailureCode.ContactIdentifierInUse));
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.Email.Should().Be("original@example.test");
        persisted.FirstName.Should().Be("Original");
        persisted.ConcurrencyStamp.Should().Be(SeedRevision);
        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditFailureEvent &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.MutationRejected);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.MutationRejected);
        events.Should().NotContain(entry =>
            entry.RedactedReason != null &&
            entry.RedactedReason.Contains("taken@example.test", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Duplicate_phone_edit_is_rejected_with_a_typed_conflict_and_no_mutation()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        await SeedConflictingUserAsync(dbContext, "other-user", null, "+14155552671");
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = CreateUpdateHandler(dbContext, CreateCache(), auditWriter);

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, "original@example.test", "+14155552671", "Changed", "User", SeedRevision),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(UsersCommandFailureCode.ContactIdentifierInUse));
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.PhoneNumber.Should().BeNull();
        persisted.FirstName.Should().Be("Original");
        persisted.ConcurrencyStamp.Should().Be(SeedRevision);
        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditFailureEvent &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.MutationRejected);
    }

    [Fact]
    public async Task Edit_without_a_revision_from_a_legacy_caller_skips_the_stale_edit_check()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = CreateUpdateHandler(dbContext, CreateCache(), auditWriter);

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, "updated@example.test", null, "Changed", "User", null),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.Email.Should().Be("updated@example.test");
        persisted.ConcurrencyStamp.Should().NotBe(SeedRevision);
    }

    [Fact]
    public async Task Edit_with_a_stale_revision_is_rejected_as_a_concurrency_conflict()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        IDistributedCache cache = CreateCache();
        UpdateManagedUserCommandHandler handler = CreateUpdateHandler(dbContext, cache, auditWriter);

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, "updated@example.test", null, "Changed", "User", "stale-revision"),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.Code == nameof(UsersCommandFailureCode.ConcurrencyFailure) &&
            error.Description == UsersCommandErrorCodes.ConcurrencyFailureDescription);
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.Email.Should().Be("original@example.test");
        persisted.FirstName.Should().Be("Original");
        persisted.ConcurrencyStamp.Should().Be(SeedRevision);
        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditFailureEvent &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.SecurityPolicyConcurrencyConflict);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.SecurityPolicyConcurrencyConflict);
        User? cached = await UserCacheExtensions.GetUserAsync(cache, ManagedUserId, CancellationToken.None);
        cached.Should().NotBeNull();
        cached!.ConcurrencyStamp.Should().Be(SeedRevision);
    }

    [Fact]
    public async Task Successful_edit_with_a_matching_revision_clears_only_the_changed_contact_confirmation_and_advances_the_revision()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync(user =>
        {
            user.EmailConfirmed = true;
            user.PhoneNumber = "+14155552671";
            user.PhoneNumberConfirmed = true;
        });
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        IDistributedCache cache = CreateCache();
        UpdateManagedUserCommandHandler handler = CreateUpdateHandler(dbContext, cache, auditWriter);

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, "updated@example.test", "+14155552671", "Changed", "User", SeedRevision),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.Email.Should().Be("updated@example.test");
        persisted.UserName.Should().Be("updated@example.test");
        persisted.NormalizedUserName.Should().Be("UPDATED@EXAMPLE.TEST");
        persisted.SecurityStamp.Should().Be("security-stamp");
        persisted.EmailConfirmed.Should().BeFalse();
        persisted.PhoneNumber.Should().Be("+14155552671");
        persisted.PhoneNumberConfirmed.Should().BeTrue();
        persisted.ConcurrencyStamp.Should().NotBeNull().And.NotBe(SeedRevision);
        User? cached = await UserCacheExtensions.GetUserAsync(cache, ManagedUserId, CancellationToken.None);
        cached.Should().NotBeNull();
        cached!.ConcurrencyStamp.Should().Be(persisted.ConcurrencyStamp);
    }

    [Fact]
    public async Task Clearing_a_contact_clears_its_confirmation_flag_and_leaves_the_unchanged_contact_confirmation()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync(user =>
        {
            user.EmailConfirmed = true;
            user.PhoneNumber = "+14155552671";
            user.PhoneNumberConfirmed = true;
        });
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = CreateUpdateHandler(dbContext, CreateCache(), auditWriter);

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, "original@example.test", null, "Changed", "User", SeedRevision),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.Email.Should().Be("original@example.test");
        persisted.EmailConfirmed.Should().BeTrue();
        persisted.PhoneNumber.Should().BeNull();
        persisted.PhoneNumberConfirmed.Should().BeFalse();
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

    private static UpdateManagedUserCommandHandler CreateUpdateHandler(
        TestUsersDbContext dbContext,
        IDistributedCache cache,
        Mock<ISecurityAuditWriter> auditWriter) => new(
        CreateUserManager(dbContext),
        CreateSender(dbContext),
        cache,
        new UsersManagementAudit(auditWriter.Object, RequestContext(), TimeProvider.System),
        RequestContext());

    private static ISender CreateSender(TestUsersDbContext dbContext)
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
            UserName = "original@example.test",
            NormalizedUserName = "ORIGINAL@EXAMPLE.TEST",
            Email = "original@example.test",
            NormalizedEmail = "ORIGINAL@EXAMPLE.TEST",
            FirstName = "Original",
            LastName = "User",
            SecurityStamp = "security-stamp",
            ConcurrencyStamp = SeedRevision
        };
        configure?.Invoke(managedUser);
        dbContext.Users.Add(managedUser);
        await dbContext.SaveChangesAsync();
        return dbContext;
    }

    private static async Task SeedConflictingUserAsync(TestUsersDbContext dbContext, string userId, string? email, string? phoneNumber)
    {
        dbContext.Users.Add(new User
        {
            Id = userId,
            UserName = email ?? phoneNumber,
            NormalizedUserName = (email ?? phoneNumber)?.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email?.ToUpperInvariant(),
            PhoneNumber = phoneNumber,
            SecurityStamp = "conflicting-security-stamp",
            ConcurrencyStamp = "conflicting-revision"
        });
        await dbContext.SaveChangesAsync();
    }

    private sealed class TestUsersDbContext(DbContextOptions<UsersDbContext> options) : UsersDbContext(options);
}
