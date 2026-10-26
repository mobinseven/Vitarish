using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Security;

public sealed class GlobalRoleAuditTests
{
    private const string UserId = "user-1";

    [Fact]
    public async Task Global_role_assignment_and_revocation_record_the_previous_and_new_role_sets()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        Mock<IAuthenticationActiveSessionQuery> activeSessions = ActiveSessions();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        IRequestContext requestContext = RequestContext();

        AddUserToRoleCommandHandler add = new(dbContext, activeSessions.Object, requestContext, auditWriter.Object);
        IdentityResult assigned = await add.Handle(
            new AddUserToRoleCommand(new User { Id = UserId }, IdentityDefinitions.PlatformAdmin),
            CancellationToken.None);

        assigned.Succeeded.Should().BeTrue();
        AuditEvents(auditWriter).Single(entry => entry.EventName == SecurityAuditEventCatalog.GlobalRoleAssignmentEvent)
            .Changes.Should().Equal(new SecurityAuditChange(
                SecurityAuditChangeFields.GlobalRole,
                IdentityDefinitions.PlatformUser,
                $"{IdentityDefinitions.PlatformAdmin},{IdentityDefinitions.PlatformUser}"));

        RemoveUserFromRoleCommandHandler remove = new(dbContext, activeSessions.Object, requestContext, auditWriter.Object);
        IdentityResult revoked = await remove.Handle(
            new RemoveUserFromRoleCommand(new User { Id = UserId }, IdentityDefinitions.PlatformAdmin),
            CancellationToken.None);

        revoked.Succeeded.Should().BeTrue();
        AuditEvents(auditWriter).Single(entry => entry.EventName == SecurityAuditEventCatalog.GlobalRoleRevocationEvent)
            .Changes.Should().Equal(new SecurityAuditChange(
                SecurityAuditChangeFields.GlobalRole,
                $"{IdentityDefinitions.PlatformAdmin},{IdentityDefinitions.PlatformUser}",
                IdentityDefinitions.PlatformUser));
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

    private static Mock<IAuthenticationActiveSessionQuery> ActiveSessions()
    {
        Mock<IAuthenticationActiveSessionQuery> query = new();
        query.Setup(value => value.HasActiveSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        return query;
    }

    private static IRequestContext RequestContext()
    {
        Mock<IRequestContext> context = new();
        context.SetupGet(value => value.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)],
            "test")));
        context.SetupGet(value => value.UserId).Returns("administrator");
        context.SetupGet(value => value.RequestId).Returns("request-1");
        context.SetupGet(value => value.TraceId).Returns("trace-1");
        return context.Object;
    }

    private static async Task<TestUsersDbContext> CreateDbContextAsync()
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        TestUsersDbContext dbContext = new(options);
        dbContext.Users.Add(new User { Id = UserId, UserName = UserId });
        IdentityRole adminRole = new() { Id = "role-admin", Name = IdentityDefinitions.PlatformAdmin, NormalizedName = IdentityDefinitions.PlatformAdmin.ToUpperInvariant() };
        IdentityRole userRole = new() { Id = "role-user", Name = IdentityDefinitions.PlatformUser, NormalizedName = IdentityDefinitions.PlatformUser.ToUpperInvariant() };
        dbContext.Roles.AddRange(adminRole, userRole);
        dbContext.UserRoles.Add(new IdentityUserRole<string> { UserId = UserId, RoleId = userRole.Id });
        await dbContext.SaveChangesAsync();
        return dbContext;
    }

    private sealed class TestUsersDbContext(DbContextOptions<UsersDbContext> options) : UsersDbContext(options);
}
