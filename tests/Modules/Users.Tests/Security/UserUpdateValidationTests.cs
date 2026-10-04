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

    private static async Task<UsersDbContext> CreateDbContextWithUserAsync(Action<User>? configure = null)
    {
        UsersDbContext dbContext = new(
            new DbContextOptionsBuilder<UsersDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options);
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
