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
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Common;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Security;

public sealed class UserUpdateValidationTests
{
    private const string UserId = "update-under-test";
    private const int OverLengthNameLength = 65;

    [Fact]
    public async Task Profile_update_succeeds_for_an_account_without_an_email_or_phone()
    {
        await using UsersDbContext dbContext = await CreateDbContextWithUserAsync(user =>
        {
            user.Email = null;
            user.NormalizedEmail = null;
            user.PhoneNumber = null;
        });
        UpdateUserProfileCommandHandler handler = CreateProfileHandler(dbContext);

        Func<Task> update = () => handler.Handle(
            new UpdateUserProfileCommand(UserId, new Dictionary<string, string> { ["Culture"] = "fa-IR" }),
            CancellationToken.None);

        await update.Should().NotThrowAsync();
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId);
        persisted.ProfilePreferences["Culture"].Should().Be("fa-IR");
    }

    [Fact]
    public async Task Profile_update_succeeds_when_the_stored_name_exceeds_the_limit()
    {
        await using UsersDbContext dbContext = await CreateDbContextWithUserAsync(user => user.LastName = new string('L', OverLengthNameLength));
        UpdateUserProfileCommandHandler handler = CreateProfileHandler(dbContext);

        Func<Task> update = () => handler.Handle(
            new UpdateUserProfileCommand(UserId, new Dictionary<string, string> { ["Culture"] = "fa-IR" }),
            CancellationToken.None);

        await update.Should().NotThrowAsync();
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId);
        persisted.ProfilePreferences["Culture"].Should().Be("fa-IR");
    }

    [Fact]
    public async Task Profile_update_keeps_owner_namespaced_entries_it_did_not_write()
    {
        await using UsersDbContext dbContext = await CreateDbContextWithUserAsync(user =>
            user.ProfilePreferences = new() { ["Culture"] = "en", ["intelligence.memory"] = "[stored]" });
        UpdateUserProfileCommandHandler handler = CreateProfileHandler(dbContext);

        // A stale client copy that lacks the stored entry, or carries an old value for it.
        await handler.Handle(
            new UpdateUserProfileCommand(UserId, new Dictionary<string, string> { ["Culture"] = "fa-IR", ["intelligence.memory"] = "[stale]" }),
            CancellationToken.None);

        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId);
        persisted.ProfilePreferences.Should().Equal(new Dictionary<string, string> { ["Culture"] = "fa-IR", ["intelligence.memory"] = "[stored]" });
    }

    [Fact]
    public async Task Name_update_succeeds_for_an_account_without_an_email_or_phone()
    {
        await using UsersDbContext dbContext = await CreateDbContextWithUserAsync(user =>
        {
            user.Email = null;
            user.NormalizedEmail = null;
            user.PhoneNumber = null;
        });
        UserManager<User> userManager = CreateUserManager(dbContext);
        User request = (await userManager.FindByIdAsync(UserId))!;
        request.FirstName = "Renamed";

        IdentityResult result = await CreateUpdateHandler(dbContext, userManager)
            .Handle(new UpdateUserCommand(request), CancellationToken.None);

        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors.Select(error => $"{error.Code}:{error.Description}")));
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId);
        persisted.FirstName.Should().Be("Renamed");
    }

    [Fact]
    public async Task Name_update_succeeds_when_the_other_stored_name_exceeds_the_limit()
    {
        await using UsersDbContext dbContext = await CreateDbContextWithUserAsync(user => user.LastName = new string('L', OverLengthNameLength));
        UserManager<User> userManager = CreateUserManager(dbContext);
        User request = (await userManager.FindByIdAsync(UserId))!;
        request.FirstName = "Renamed";

        IdentityResult result = await CreateUpdateHandler(dbContext, userManager)
            .Handle(new UpdateUserCommand(request), CancellationToken.None);

        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors.Select(error => $"{error.Code}:{error.Description}")));
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId);
        persisted.FirstName.Should().Be("Renamed");
        persisted.LastName.Should().Be(new string('L', OverLengthNameLength));
    }

    [Fact]
    public async Task Name_update_succeeds_for_a_detached_copy_of_the_user()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        await using UsersDbContext dbContext = await CreateDbContextWithUserAsync(databaseName: databaseName);
        // As in the host, the Users context is transient, so Identity's store has its own instance.
        await using UsersDbContext storeContext = CreateDbContext(databaseName);
        UserManager<User> userManager = CreateUserManager(storeContext);
        // The user cache hands out copies no context tracks.
        User request = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId);
        request.FirstName = "Renamed";

        IdentityResult result = await CreateUpdateHandler(dbContext, userManager)
            .Handle(new UpdateUserCommand(request), CancellationToken.None);

        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors.Select(error => $"{error.Code}:{error.Description}")));
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId);
        persisted.FirstName.Should().Be("Renamed");
        persisted.LastName.Should().Be("Last");
    }

    [Fact]
    public async Task The_user_cache_holds_no_credential_or_lockout_state()
    {
        IDistributedCache cache = CreateCache();
        User user = new()
        {
            Id = UserId,
            UserName = "cached-user",
            Email = "cached@example.test",
            PasswordHash = "stored-password-hash",
            SecurityStamp = "stored-security-stamp",
            LockoutEnabled = true,
            LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5),
            AccessFailedCount = 3,
            ConcurrencyStamp = "concurrency-stamp"
        };

        await UserCacheExtensions.SetUserAsync(cache, user, CancellationToken.None);
        string payload = System.Text.Encoding.UTF8.GetString((await cache.GetAsync($"users:{UserId}"))!);
        User cached = (await UserCacheExtensions.GetUserAsync(cache, UserId, CancellationToken.None))!;

        payload.Should().NotContain("stored-password-hash").And.NotContain("stored-security-stamp");
        cached.PasswordHash.Should().BeNull();
        // IdentityUser's constructor gives every new instance a random stamp; the stored one never reaches the cache.
        cached.SecurityStamp.Should().NotBe("stored-security-stamp");
        cached.LockoutEnd.Should().BeNull();
        cached.AccessFailedCount.Should().Be(0);
        cached.Email.Should().Be("cached@example.test");
        cached.ConcurrencyStamp.Should().Be("concurrency-stamp");
    }

    [Fact]
    public async Task Name_update_from_a_cached_copy_keeps_the_stored_credentials()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        await using UsersDbContext dbContext = await CreateDbContextWithUserAsync(databaseName: databaseName);
        await using (UsersDbContext seed = CreateDbContext(databaseName))
        {
            User stored = await seed.Users.SingleAsync(user => user.Id == UserId);
            stored.PasswordHash = "stored-password-hash";
            stored.SecurityStamp = "stored-security-stamp";
            stored.AccessFailedCount = 2;
            _ = await seed.SaveChangesAsync();
        }
        IDistributedCache cache = CreateCache();
        await UserCacheExtensions.SetUserAsync(cache, await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId), CancellationToken.None);
        User request = (await UserCacheExtensions.GetUserAsync(cache, UserId, CancellationToken.None))!;
        request.FirstName = "Renamed";
        await using UsersDbContext storeContext = CreateDbContext(databaseName);

        IdentityResult result = await CreateUpdateHandler(dbContext, CreateUserManager(storeContext))
            .Handle(new UpdateUserCommand(request), CancellationToken.None);

        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors.Select(error => $"{error.Code}:{error.Description}")));
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId);
        persisted.FirstName.Should().Be("Renamed");
        persisted.PasswordHash.Should().Be("stored-password-hash");
        persisted.SecurityStamp.Should().Be("stored-security-stamp");
        persisted.AccessFailedCount.Should().Be(2);
    }

    [Fact]
    public async Task Name_update_from_a_stale_detached_copy_is_refused_without_overwriting()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        await using UsersDbContext dbContext = await CreateDbContextWithUserAsync(databaseName: databaseName);
        User stale = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId);
        await using (UsersDbContext otherWriter = CreateDbContext(databaseName))
        {
            UserManager<User> otherManager = CreateUserManager(otherWriter);
            User current = (await otherManager.FindByIdAsync(UserId))!;
            current.LastName = "Changed";
            (await otherManager.UpdateAsync(current)).Succeeded.Should().BeTrue();
        }
        await using UsersDbContext storeContext = CreateDbContext(databaseName);
        stale.FirstName = "Renamed";

        IdentityResult result = await CreateUpdateHandler(dbContext, CreateUserManager(storeContext))
            .Handle(new UpdateUserCommand(stale), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.Code == nameof(UsersCommandFailureCode.ConcurrencyFailure));
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId);
        persisted.FirstName.Should().Be("First");
        persisted.LastName.Should().Be("Changed");
    }

    [Fact]
    public async Task Name_update_to_an_over_length_value_is_rejected()
    {
        await using UsersDbContext dbContext = await CreateDbContextWithUserAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UserManager<User> userManager = CreateUserManager(dbContext);
        User request = (await userManager.FindByIdAsync(UserId))!;
        request.LastName = new string('L', OverLengthNameLength);

        IdentityResult result = await CreateUpdateHandler(dbContext, userManager, auditWriter)
            .Handle(new UpdateUserCommand(request), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        IdentityError error = result.Errors.Should().ContainSingle().Subject;
        error.Code.Should().Be(nameof(UsersCommandFailureCode.InvalidContact));
        error.Description.Should().Be(UsersCommandErrorCodes.InvalidContactDescription);
        User persisted = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == UserId);
        persisted.LastName.Should().Be("Last");
        AuditEvents(auditWriter).Should().ContainSingle(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserEditFailureEvent &&
            entry.SubjectId == UserId &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.RedactedReason == SecurityAuditReasonCatalog.MutationRejected);
    }

    [Fact]
    public async Task Email_update_to_an_invalid_value_is_rejected()
    {
        await using UsersDbContext dbContext = await CreateDbContextWithUserAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UserManager<User> userManager = CreateUserManager(dbContext);
        User request = (await userManager.FindByIdAsync(UserId))!;
        request.Email = "not-an-email";
        request.NormalizedEmail = "NOT-AN-EMAIL";

        IdentityResult result = await CreateUpdateHandler(dbContext, userManager, auditWriter)
            .Handle(new UpdateUserCommand(request), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.Code == nameof(UsersCommandFailureCode.InvalidContact));
        AuditEvents(auditWriter).Should().Contain(entry =>
            entry.EventName == SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent &&
            entry.SubjectId == UserId &&
            entry.Outcome == SecurityAuditOutcome.Denied);
    }

    [Fact]
    public async Task Profile_command_validator_still_rejects_a_changed_value_over_the_limit()
    {
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        UpdateUserProfileCommandValidator validator = new(CreateAudit(auditWriter));

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(new UpdateUserProfileCommand(
            UserId,
            new Dictionary<string, string> { ["Culture"] = "fa-IR" },
            IdentificationCode: new string('x', UserIdentityNormalization.IdentificationCodeMaxLength + 1)));

        result.IsValid.Should().BeFalse();
        auditWriter.Invocations.Should().ContainSingle();
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
        context.SetupGet(value => value.UserId).Returns("platform-admin");
        context.SetupGet(value => value.RequestId).Returns("request-id");
        context.SetupGet(value => value.TraceId).Returns("trace-id");
        return context.Object;
    }

    private static UsersManagementAudit CreateAudit(Mock<ISecurityAuditWriter> auditWriter) =>
        new(auditWriter.Object, RequestContext(), TimeProvider.System);

    private static IDistributedCache CreateCache() => new MemoryDistributedCache(
        Options.Create(new MemoryDistributedCacheOptions()));

    private static UpdateUserCommandHandler CreateUpdateHandler(UsersDbContext dbContext, UserManager<User> userManager, Mock<ISecurityAuditWriter>? auditWriter = null) => new(
        dbContext,
        userManager,
        CreateCache(),
        Mock.Of<IAuthenticationActiveSessionQuery>(),
        CreateAudit(auditWriter ?? AuditWriter()));

    private static UpdateUserProfileCommandHandler CreateProfileHandler(UsersDbContext dbContext) => new(
        CreateUserManager(dbContext),
        CreateCache(),
        CreateAudit(AuditWriter()));

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

    private static UsersDbContext CreateDbContext(string databaseName) => new(
        new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options);

    private static async Task<UsersDbContext> CreateDbContextWithUserAsync(Action<User>? configure = null, string? databaseName = null)
    {
        UsersDbContext dbContext = CreateDbContext(databaseName ?? Guid.NewGuid().ToString("N"));
        User user = new()
        {
            Id = UserId,
            UserName = "update-user",
            NormalizedUserName = "UPDATE-USER",
            Email = "user@example.test",
            NormalizedEmail = "USER@EXAMPLE.TEST",
            FirstName = "First",
            LastName = "Last",
            SecurityStamp = "security-under-test",
            ConcurrencyStamp = "update-revision"
        };
        configure?.Invoke(user);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        dbContext.Entry(user).State = EntityState.Detached;
        return dbContext;
    }
}
