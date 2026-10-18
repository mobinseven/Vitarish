using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Security;

public sealed class GrantInitialPlatformAdministratorCommandHandlerTests
{
    private const string FirstUserId = "first-user";
    private const string OtherUserId = "other-user";
    private const string PlatformAdminRoleId = "role-platform-admin";

    [Fact]
    public async Task The_first_verified_account_becomes_platform_administrator_and_is_audited()
    {
        await using UsersDbContext dbContext = await CreateDbContextAsync(existingAdministrator: null);
        List<SecurityAuditEvent> events = [];

        bool granted = await CreateHandler(dbContext, events).Handle(new GrantInitialPlatformAdministratorCommand(FirstUserId), CancellationToken.None);

        granted.Should().BeTrue();
        (await AdministratorsAsync(dbContext)).Should().Equal(FirstUserId);
        events.Should().ContainSingle(audit =>
            audit.EventName == SecurityAuditEventCatalog.GlobalRoleAssignmentEvent &&
            audit.Outcome == SecurityAuditOutcome.Succeeded &&
            audit.SubjectId == FirstUserId &&
            audit.RedactedReason == SecurityAuditReasonCatalog.InitialPlatformAdministrator);
    }

    [Fact]
    public async Task A_later_account_is_not_granted_once_an_administrator_exists()
    {
        await using UsersDbContext dbContext = await CreateDbContextAsync(existingAdministrator: OtherUserId);
        List<SecurityAuditEvent> events = [];

        bool granted = await CreateHandler(dbContext, events).Handle(new GrantInitialPlatformAdministratorCommand(FirstUserId), CancellationToken.None);

        granted.Should().BeFalse();
        (await AdministratorsAsync(dbContext)).Should().Equal(OtherUserId);
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task An_account_with_an_active_session_is_denied_as_a_role_mutation_conflict()
    {
        await using UsersDbContext dbContext = await CreateDbContextAsync(existingAdministrator: null);
        List<SecurityAuditEvent> events = [];

        bool granted = await CreateHandler(dbContext, events, hasActiveSession: true)
            .Handle(new GrantInitialPlatformAdministratorCommand(FirstUserId), CancellationToken.None);

        granted.Should().BeFalse();
        (await AdministratorsAsync(dbContext)).Should().BeEmpty();
        events.Should().ContainSingle(audit =>
            audit.Outcome == SecurityAuditOutcome.Denied &&
            audit.RedactedReason == SecurityAuditReasonCatalog.ActiveSessionConflict);
    }

    private static GrantInitialPlatformAdministratorCommandHandler CreateHandler(
        UsersDbContext dbContext,
        List<SecurityAuditEvent> events,
        bool hasActiveSession = false)
    {
        Mock<IAuthenticationActiveSessionQuery> sessions = new();
        sessions.Setup(value => value.HasActiveSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(hasActiveSession);
        Mock<ISecurityAuditWriter> audit = new();
        audit.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAuditEvent, CancellationToken>((auditEvent, _) => events.Add(auditEvent))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IRequestContext> context = new();
        context.SetupGet(value => value.RequestId).Returns("request-1");
        return new GrantInitialPlatformAdministratorCommandHandler(dbContext, sessions.Object, context.Object, audit.Object);
    }

    private static async Task<UsersDbContext> CreateDbContextAsync(string? existingAdministrator)
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        UsersDbContext dbContext = new(options);
        dbContext.Users.AddRange(
            new User { Id = FirstUserId, UserName = FirstUserId },
            new User { Id = OtherUserId, UserName = OtherUserId });
        dbContext.Roles.Add(new IdentityRole { Id = PlatformAdminRoleId, Name = IdentityDefinitions.PlatformAdmin });
        if (existingAdministrator is not null)
            dbContext.UserRoles.Add(new IdentityUserRole<string> { UserId = existingAdministrator, RoleId = PlatformAdminRoleId });
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        return dbContext;
    }

    private static Task<List<string>> AdministratorsAsync(UsersDbContext dbContext) =>
        dbContext.UserRoles.Where(value => value.RoleId == PlatformAdminRoleId).Select(value => value.UserId).ToListAsync();
}
