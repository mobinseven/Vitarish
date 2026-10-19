using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Security;

public sealed class AssignNewTenantOwnerCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    [Fact]
    public async Task Creates_both_tenant_roles_assigns_the_user_as_owner_and_records_a_succeeded_audit_event()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        Mock<ISecurityAuditWriter> audit = AuditWriter();
        AssignNewTenantOwnerCommandHandler handler = CreateHandler(dbContext, audit.Object);

        AssignNewTenantOwnerResult result = await handler.Handle(
            new AssignNewTenantOwnerCommand("creator", TenantId),
            CancellationToken.None);

        result.Should().Be(AssignNewTenantOwnerResult.Assigned);

        string ownerRoleName = TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantOwner);
        string userRoleName = TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantUser);
        List<IdentityRole> roles = await dbContext.Roles.AsNoTracking().ToListAsync();
        roles.Select(role => role.Name).Should().BeEquivalentTo([ownerRoleName, userRoleName]);

        IdentityRole ownerRole = roles.Single(role => role.Name == ownerRoleName);
        IdentityUserRole<string> membership = await dbContext.UserRoles.AsNoTracking().SingleAsync();
        membership.UserId.Should().Be("creator");
        membership.RoleId.Should().Be(ownerRole.Id);

        audit.Verify(writer => writer.WriteAsync(
            It.Is<SecurityAuditEvent>(entry =>
                entry.Outcome == SecurityAuditOutcome.Succeeded &&
                entry.TenantId == TenantId.ToString()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Returns_already_owner_without_duplicating_roles_or_memberships_when_repeated()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        AssignNewTenantOwnerCommandHandler handler = CreateHandler(dbContext, AuditWriter().Object);

        AssignNewTenantOwnerResult first = await handler.Handle(
            new AssignNewTenantOwnerCommand("creator", TenantId),
            CancellationToken.None);
        AssignNewTenantOwnerResult second = await handler.Handle(
            new AssignNewTenantOwnerCommand("creator", TenantId),
            CancellationToken.None);

        first.Should().Be(AssignNewTenantOwnerResult.Assigned);
        second.Should().Be(AssignNewTenantOwnerResult.AlreadyOwner);
        (await dbContext.Roles.CountAsync()).Should().Be(2);
        (await dbContext.UserRoles.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Rejects_assignment_when_another_user_already_holds_a_role_in_the_tenant()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        IdentityRole userRole = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantUser),
            NormalizedName = TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantUser).ToUpperInvariant()
        };
        dbContext.Roles.Add(userRole);
        dbContext.UserRoles.Add(new IdentityUserRole<string> { UserId = "other", RoleId = userRole.Id });
        await dbContext.SaveChangesAsync();

        AssignNewTenantOwnerCommandHandler handler = CreateHandler(dbContext, AuditWriter().Object);

        AssignNewTenantOwnerResult result = await handler.Handle(
            new AssignNewTenantOwnerCommand("creator", TenantId),
            CancellationToken.None);

        result.Should().Be(AssignNewTenantOwnerResult.TenantHasMembers);
        (await dbContext.UserRoles.AnyAsync(membership => membership.UserId == "creator")).Should().BeFalse();
    }

    [Fact]
    public async Task Returns_user_not_found_for_an_unknown_user()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        AssignNewTenantOwnerCommandHandler handler = CreateHandler(dbContext, AuditWriter().Object);

        AssignNewTenantOwnerResult result = await handler.Handle(
            new AssignNewTenantOwnerCommand("missing-user", TenantId),
            CancellationToken.None);

        result.Should().Be(AssignNewTenantOwnerResult.UserNotFound);
        (await dbContext.Roles.CountAsync()).Should().Be(0);
        (await dbContext.UserRoles.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Refuses_assignment_while_the_user_has_an_active_session()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        AssignNewTenantOwnerCommandHandler handler = CreateHandler(dbContext, AuditWriter().Object, hasActiveSession: true);

        AssignNewTenantOwnerResult result = await handler.Handle(
            new AssignNewTenantOwnerCommand("creator", TenantId),
            CancellationToken.None);

        result.Should().Be(AssignNewTenantOwnerResult.ActiveSessionConflict);
        (await dbContext.UserRoles.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Revokes_new_tenant_owner_memberships_and_roles_and_rejects_when_another_member_exists()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        AssignNewTenantOwnerCommandHandler assignHandler = CreateHandler(dbContext, AuditWriter().Object);
        RevokeNewTenantOwnerCommandHandler revokeHandler = new(dbContext);

        _ = await assignHandler.Handle(new AssignNewTenantOwnerCommand("creator", TenantId), CancellationToken.None);
        await revokeHandler.Handle(new RevokeNewTenantOwnerCommand("creator", TenantId), CancellationToken.None);

        (await dbContext.UserRoles.CountAsync()).Should().Be(0);
        (await dbContext.Roles.CountAsync()).Should().Be(0);

        _ = await assignHandler.Handle(new AssignNewTenantOwnerCommand("creator", TenantId), CancellationToken.None);
        string userRoleName = TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantUser);
        IdentityRole userRole = await dbContext.Roles.SingleAsync(role => role.Name == userRoleName);
        dbContext.UserRoles.Add(new IdentityUserRole<string> { UserId = "other", RoleId = userRole.Id });
        await dbContext.SaveChangesAsync();

        Func<Task> act = () => revokeHandler.Handle(new RevokeNewTenantOwnerCommand("creator", TenantId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await dbContext.Roles.CountAsync()).Should().Be(2);
    }

    private static AssignNewTenantOwnerCommandHandler CreateHandler(
        UsersDbContext dbContext,
        ISecurityAuditWriter auditWriter,
        bool hasActiveSession = false)
    {
        Mock<IAuthenticationActiveSessionQuery> activeSessions = new();
        activeSessions.Setup(query => query.HasActiveSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hasActiveSession);

        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.UserId).Returns("creator");
        requestContext.SetupGet(context => context.RequestId).Returns(Guid.NewGuid().ToString("N"));
        return new AssignNewTenantOwnerCommandHandler(
            dbContext,
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            activeSessions.Object,
            auditWriter,
            requestContext.Object);
    }

    private static Mock<ISecurityAuditWriter> AuditWriter()
    {
        Mock<ISecurityAuditWriter> audit = new();
        audit.Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        return audit;
    }

    private static async Task<TestUsersDbContext> CreateDbContextAsync()
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        TestUsersDbContext dbContext = new(options);
        dbContext.Users.AddRange(
            new User { Id = "creator", UserName = "creator", Email = "creator@example.com" },
            new User { Id = "other", UserName = "other", Email = "other@example.com" });
        await dbContext.SaveChangesAsync();
        return dbContext;
    }

    private sealed class TestUsersDbContext(DbContextOptions<UsersDbContext> options) : UsersDbContext(options);
}
