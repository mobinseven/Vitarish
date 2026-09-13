using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Security;

public sealed class ReplaceTenantRoleCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private const string UserId = "user-1";

    [Fact]
    public async Task Replacement_removes_conflicting_memberships_and_audits_redacted_before_after_state()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync(includeConflictingMemberships: true);
        Mock<ISecurityAuditWriter> audit = AuditWriter();
        ReplaceTenantRoleCommandHandler handler = CreateHandler(dbContext, audit);

        IdentityResult result = await handler.Handle(
            new ReplaceTenantRoleCommand(UserId, TenantId, IdentityDefinitions.TenantOwner),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        await AssertMembershipsAsync(dbContext, IdentityDefinitions.TenantOwner);
        SecurityAuditEvent auditEvent = audit.Invocations
            .Select(invocation => invocation.Arguments[0])
            .OfType<SecurityAuditEvent>()
            .Single();
        auditEvent.Outcome.Should().Be(SecurityAuditOutcome.Succeeded);
        auditEvent.RedactedReason.Should().Be("tenant_role_replacement:before=multiple;after=tenant_owner");
        auditEvent.RedactedReason.Should().NotContain(UserId);
        auditEvent.RedactedReason.Should().NotContain(TenantId.ToString());
    }

    [Fact]
    public async Task Repeated_replacement_is_idempotent_and_revoke_removes_the_membership()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync(includeConflictingMemberships: false);
        Mock<ISecurityAuditWriter> audit = AuditWriter();
        ReplaceTenantRoleCommandHandler handler = CreateHandler(dbContext, audit);

        (await handler.Handle(new ReplaceTenantRoleCommand(UserId, TenantId, IdentityDefinitions.TenantUser), CancellationToken.None)).Succeeded.Should().BeTrue();
        (await handler.Handle(new ReplaceTenantRoleCommand(UserId, TenantId, IdentityDefinitions.TenantUser), CancellationToken.None)).Succeeded.Should().BeTrue();
        await AssertMembershipsAsync(dbContext, IdentityDefinitions.TenantUser);

        (await handler.Handle(new ReplaceTenantRoleCommand(UserId, TenantId, null), CancellationToken.None)).Succeeded.Should().BeTrue();
        await AssertMembershipsAsync(dbContext);
    }

    [Fact]
    public async Task Invalid_role_and_active_session_are_rejected_before_user_role_change()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync(includeConflictingMemberships: false);
        Mock<ISecurityAuditWriter> audit = AuditWriter();
        ReplaceTenantRoleCommandHandler handler = CreateHandler(dbContext, audit, hasActiveSession: true);

        IdentityResult activeSessionResult = await handler.Handle(
            new ReplaceTenantRoleCommand(UserId, TenantId, IdentityDefinitions.TenantOwner),
            CancellationToken.None);
        activeSessionResult.Errors.Single().Code.Should().Be(UsersCommandErrorCodes.ActiveSessionConflict);
        await AssertMembershipsAsync(dbContext);

        handler = CreateHandler(dbContext, audit, hasActiveSession: false);
        IdentityResult invalidRoleResult = await handler.Handle(
            new ReplaceTenantRoleCommand(UserId, TenantId, IdentityDefinitions.PlatformAdmin),
            CancellationToken.None);
        invalidRoleResult.Errors.Single().Code.Should().Be(UsersCommandErrorCodes.InvalidTenantRole);
        await AssertMembershipsAsync(dbContext);
    }

    [Fact]
    public async Task Generic_role_writer_rejects_tenant_role_assignment()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync(includeConflictingMemberships: false);
        Mock<IAuthenticationActiveSessionQuery> sessions = new();
        sessions.Setup(value => value.HasActiveSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        AddUserToRoleCommandHandler handler = new(dbContext, sessions.Object, RequestContext(), AuditWriter().Object);

        IdentityResult result = await handler.Handle(
            new AddUserToRoleCommand(new User { Id = UserId }, IdentityDefinitions.TenantOwner, TenantId),
            CancellationToken.None);

        result.Errors.Single().Code.Should().Be(UsersCommandErrorCodes.InvalidTenantRole);
    }

    [Fact]
    public async Task Third_concurrency_conflict_returns_typed_conflict_and_audits_bounded_retry()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync(includeConflictingMemberships: false);
        dbContext.ConcurrencyFailuresRemaining = 3;
        Mock<ISecurityAuditWriter> audit = AuditWriter();
        ReplaceTenantRoleCommandHandler handler = CreateHandler(dbContext, audit);

        IdentityResult result = await handler.Handle(
            new ReplaceTenantRoleCommand(UserId, TenantId, IdentityDefinitions.TenantOwner),
            CancellationToken.None);

        result.Errors.Single().Code.Should().Be(UsersCommandErrorCodes.TenantRoleConflict);
        dbContext.SaveAttempts.Should().Be(3);
        audit.Invocations.Select(invocation => invocation.Arguments[0]).OfType<SecurityAuditEvent>()
            .Single().RedactedReason.Should().Be(SecurityAuditReasonCatalog.TenantRoleConcurrencyConflict);
        await AssertMembershipsAsync(dbContext);
    }

    private static ReplaceTenantRoleCommandHandler CreateHandler(
        TestUsersDbContext dbContext,
        Mock<ISecurityAuditWriter> audit,
        bool hasActiveSession = false)
    {
        Mock<IAuthenticationActiveSessionQuery> sessions = new();
        sessions.Setup(value => value.HasActiveSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(hasActiveSession);
        return new ReplaceTenantRoleCommandHandler(dbContext, sessions.Object, audit.Object, RequestContext());
    }

    private static Mock<ISecurityAuditWriter> AuditWriter()
    {
        Mock<ISecurityAuditWriter> audit = new();
        audit.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        return audit;
    }

    private static IRequestContext RequestContext()
    {
        Mock<IRequestContext> context = new();
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)],
            "test"));
        context.SetupGet(value => value.User).Returns(principal);
        context.SetupGet(value => value.TenantId).Returns(TenantId.ToString());
        context.SetupGet(value => value.UserId).Returns("administrator");
        context.SetupGet(value => value.RequestId).Returns("request-1");
        context.SetupGet(value => value.TraceId).Returns("trace-1");
        return context.Object;
    }

    private static async Task<TestUsersDbContext> CreateDbContextAsync(bool includeConflictingMemberships)
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        TestUsersDbContext dbContext = new(options);
        dbContext.Users.Add(new User { Id = UserId, UserName = UserId, ConcurrencyStamp = "stamp-1" });
        IdentityRole ownerRole = new() { Id = "role-owner", Name = TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantOwner), NormalizedName = "OWNER" };
        IdentityRole userRole = new() { Id = "role-user", Name = TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantUser), NormalizedName = "USER" };
        dbContext.Roles.AddRange(ownerRole, userRole);
        if (includeConflictingMemberships)
        {
            dbContext.UserRoles.AddRange(
                new IdentityUserRole<string> { UserId = UserId, RoleId = ownerRole.Id },
                new IdentityUserRole<string> { UserId = UserId, RoleId = userRole.Id });
        }
        await dbContext.SaveChangesAsync();
        dbContext.SaveAttempts = 0;
        return dbContext;
    }

    private static async Task AssertMembershipsAsync(TestUsersDbContext dbContext, string? expectedRoleSuffix = null)
    {
        List<string> names = await dbContext.UserRoles
            .Where(value => value.UserId == UserId)
            .Join(dbContext.Roles, value => value.RoleId, role => role.Id, (_, role) => role.Name!)
            .ToListAsync();
        if (expectedRoleSuffix is null)
        {
            names.Should().BeEmpty();
            return;
        }

        names.Should().Equal(TenantRoleDefinitions.BuildRoleName(TenantId, expectedRoleSuffix));
    }

    private sealed class TestUsersDbContext(DbContextOptions<UsersDbContext> options) : UsersDbContext(options)
    {
        public int ConcurrencyFailuresRemaining { get; set; }
        public int SaveAttempts { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveAttempts++;
            if (ConcurrencyFailuresRemaining > 0)
            {
                ConcurrencyFailuresRemaining--;
                throw new DbUpdateConcurrencyException();
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
