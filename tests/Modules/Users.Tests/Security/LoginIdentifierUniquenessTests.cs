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
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Tests.Security;

public sealed class LoginIdentifierUniquenessTests
{
    private const string ManagedUserId = "managed-user";
    private const string SeedRevision = "seed-revision";

    [Fact]
    public async Task Admin_edit_rejects_an_email_equal_to_another_accounts_user_name()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        await SeedUserAsync(dbContext, "other-user", userName: "taken@example.test", email: null, phoneNumber: null);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = CreateUpdateHandler(dbContext, auditWriter);

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, "taken@example.test", null, null, null, SeedRevision),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(UsersCommandFailureCode.ContactIdentifierInUse));
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.Email.Should().Be("original@example.test");
        persisted.FirstName.Should().Be("Original");
        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditFailureEvent &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.MutationRejected);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent &&
            entry.SubjectId == ManagedUserId &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.MutationRejected);
    }

    [Fact]
    public async Task Admin_edit_rejects_a_phone_equal_to_another_accounts_user_name()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        await SeedUserAsync(dbContext, "other-user", userName: "+989123456789", email: null, phoneNumber: null);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = CreateUpdateHandler(dbContext, auditWriter);

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, null, "09123456789", null, null, SeedRevision),
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(UsersCommandFailureCode.ContactIdentifierInUse));
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public async Task Admin_edit_passes_when_the_new_email_equals_the_accounts_own_user_name()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        User managed = await dbContext.Users.SingleAsync(user => user.Id == ManagedUserId);
        managed.Email = null;
        managed.NormalizedEmail = null;
        await dbContext.SaveChangesAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateManagedUserCommandHandler handler = CreateUpdateHandler(dbContext, auditWriter);

        IdentityResult result = await handler.Handle(
            new UpdateManagedUserCommand(ManagedUserId, "original@example.test", null, null, null, SeedRevision),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.Email.Should().Be("original@example.test");
    }

    [Fact]
    public async Task Set_phone_rejects_a_number_owned_by_another_account_and_audits_the_denial()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        await SeedUserAsync(dbContext, "other-user", userName: null, email: null, phoneNumber: "+989123456789");
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        SetUserPhoneNumberCommandHandler handler = new(
            CreateUserManager(dbContext), CreateSender(dbContext), CreateCache(), CreateAudit(auditWriter));

        IdentityResult result = await handler.Handle(
            new SetUserPhoneNumberCommand(ManagedUserId, "09123456789"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(UsersCommandFailureCode.ContactIdentifierInUse));
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.PhoneNumber.Should().BeNull();
        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditFailureEvent &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.MutationRejected);
        events.Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent &&
            entry.SubjectId == ManagedUserId &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.MutationRejected);
    }

    [Fact]
    public async Task Set_email_rejects_an_address_owned_by_another_account()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextWithManagedUserAsync();
        await SeedUserAsync(dbContext, "other-user", userName: null, email: "taken@example.test", phoneNumber: null);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        SetUserEmailCommandHandler handler = new(
            CreateUserManager(dbContext), CreateSender(dbContext), CreateCache(), CreateAudit(auditWriter));

        IdentityResult result = await handler.Handle(
            new SetUserEmailCommand(ManagedUserId, "TAKEN@example.test"), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle()
            .Which.Code.Should().Be(nameof(UsersCommandFailureCode.ContactIdentifierInUse));
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == ManagedUserId);
        persisted.Email.Should().Be("original@example.test");
    }

    [Fact]
    public async Task Create_with_phone_rejects_a_number_owned_by_another_account()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, "other-user", userName: "+989123456789", email: null, phoneNumber: null);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        CreateUserWithPhoneCommandHandler handler = new(
            CreateUserManager(dbContext), CreateSender(dbContext), CreateCache(), CreateAudit(auditWriter));

        User? user = await handler.Handle(
            new CreateUserWithPhoneCommand("09123456789", "New", "Member", RejectIfAlreadyExists: true), CancellationToken.None);

        user.Should().BeNull();
        (await dbContext.Users.CountAsync()).Should().Be(1);
        SecurityAuditEvent failed = AuditEvents(auditWriter).Single();
        failed.EventName.Should().Be(SecurityAuditEventCatalog.UserCreationFailureEvent);
        failed.Outcome.Should().Be(SecurityAuditOutcome.Denied);
        failed.RedactedReason.Should().Be(SecurityAuditReasonCatalog.MutationRejected);
    }

    [Fact]
    public async Task Create_with_email_rejects_an_address_owned_by_another_account()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, "other-user", userName: null, email: "taken@example.test", phoneNumber: null);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        CreateUserWithEmailCommandHandler handler = new(
            CreateUserManager(dbContext), CreateSender(dbContext), CreateCache(), CreateAudit(auditWriter));

        User? user = await handler.Handle(
            new CreateUserWithEmailCommand("Taken@example.test", "New", "Member", RejectIfAlreadyExists: true), CancellationToken.None);

        user.Should().BeNull();
        (await dbContext.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Create_with_phone_does_not_return_an_account_that_only_matches_by_user_name()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, "other-user", userName: "+989123456789", email: null, phoneNumber: null);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        CreateUserWithPhoneCommandHandler handler = new(
            CreateUserManager(dbContext), CreateSender(dbContext), CreateCache(), CreateAudit(auditWriter));

        User? user = await handler.Handle(
            new CreateUserWithPhoneCommand("09123456789", "New", "Member"), CancellationToken.None);

        user.Should().BeNull();
        (await dbContext.Users.CountAsync()).Should().Be(1);
        SecurityAuditEvent failed = AuditEvents(auditWriter).Single();
        failed.EventName.Should().Be(SecurityAuditEventCatalog.UserCreationFailureEvent);
        failed.Outcome.Should().Be(SecurityAuditOutcome.Denied);
        failed.RedactedReason.Should().Be(SecurityAuditReasonCatalog.MutationRejected);
    }

    [Fact]
    public async Task Create_with_email_does_not_return_an_account_that_only_matches_by_user_name()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, "other-user", userName: "taken@example.test", email: null, phoneNumber: null);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        CreateUserWithEmailCommandHandler handler = new(
            CreateUserManager(dbContext), CreateSender(dbContext), CreateCache(), CreateAudit(auditWriter));

        User? user = await handler.Handle(
            new CreateUserWithEmailCommand("taken@example.test", "New", "Member"), CancellationToken.None);

        user.Should().BeNull();
        (await dbContext.Users.CountAsync()).Should().Be(1);
        SecurityAuditEvent failed = AuditEvents(auditWriter).Single();
        failed.EventName.Should().Be(SecurityAuditEventCatalog.UserCreationFailureEvent);
        failed.Outcome.Should().Be(SecurityAuditOutcome.Denied);
        failed.RedactedReason.Should().Be(SecurityAuditReasonCatalog.MutationRejected);
    }

    [Fact]
    public async Task Create_with_a_differently_cased_email_returns_the_existing_account()
    {
        await using TestUsersDbContext dbContext = CreateDbContext();
        await SeedUserAsync(dbContext, "existing-user", userName: "existing-user", email: "owner@example.test", phoneNumber: null);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        CreateUserWithEmailCommandHandler handler = new(
            CreateUserManager(dbContext), CreateSender(dbContext), CreateCache(), CreateAudit(auditWriter));

        User? user = await handler.Handle(
            new CreateUserWithEmailCommand("Owner@Example.Test", "New", "Member"), CancellationToken.None);

        user.Should().NotBeNull();
        user!.Id.Should().Be("existing-user");
        user.Email.Should().Be("owner@example.test");
        (await dbContext.Users.CountAsync()).Should().Be(1);
        AuditEvents(auditWriter).Should().BeEmpty();
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

    private static UsersManagementAudit CreateAudit(Mock<ISecurityAuditWriter> auditWriter) =>
        new(auditWriter.Object, RequestContext(), TimeProvider.System);

    private static UpdateManagedUserCommandHandler CreateUpdateHandler(TestUsersDbContext dbContext, Mock<ISecurityAuditWriter> auditWriter) => new(
        CreateUserManager(dbContext),
        CreateSender(dbContext),
        CreateCache(),
        CreateAudit(auditWriter),
        RequestContext());

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
            FirstName = "Original",
            LastName = "User",
            SecurityStamp = "security-stamp",
            ConcurrencyStamp = SeedRevision
        });
        await dbContext.SaveChangesAsync();
        return dbContext;
    }

    private static async Task SeedUserAsync(TestUsersDbContext dbContext, string userId, string? userName, string? email, string? phoneNumber)
    {
        dbContext.Users.Add(new User
        {
            Id = userId,
            UserName = userName ?? email ?? phoneNumber,
            NormalizedUserName = (userName ?? email ?? phoneNumber)?.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email?.ToUpperInvariant(),
            PhoneNumber = phoneNumber,
            SecurityStamp = "other-security-stamp",
            ConcurrencyStamp = "other-revision"
        });
        await dbContext.SaveChangesAsync();
    }

    private sealed class TestUsersDbContext(DbContextOptions<UsersDbContext> options) : UsersDbContext(options);
}
