using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Testcontainers.PostgreSql;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Data;

public sealed class TenantMemberRemovalPersistenceTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [DockerFact]
    public async Task Removal_runs_under_the_retrying_execution_strategy_production_enables()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connection = await PostgresTestSupport.CreateDatabaseAsync(container);
        await using UsersDbContext db = new(new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(connection, provider => provider.EnableRetryOnFailure()).Options);
        await db.Database.MigrateAsync();
        IdentityRole staffRole = Role(IdentityDefinitions.TenantUser);
        db.Roles.AddRange(Role(IdentityDefinitions.TenantOwner), staffRole);
        db.Users.Add(new User { Id = "staff", UserName = "staff", NormalizedUserName = "STAFF" });
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = "staff", RoleId = staffRole.Id });
        await db.SaveChangesAsync();
        Mock<ISecurityAuditWriter> audit = new();
        audit.Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IAuthenticationActiveSessionQuery> sessions = new();
        sessions.Setup(query => query.HasActiveSessionAsync("staff", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        RemoveTenantMemberCommandHandler handler = new(db, sessions.Object, audit.Object, OwnerContext());

        IdentityResult result = await handler.Handle(new RemoveTenantMemberCommand("staff"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        (await db.UserRoles.AsNoTracking().AnyAsync(membership => membership.UserId == "staff")).Should().BeFalse();
        audit.Verify(writer => writer.WriteAsync(
            It.Is<SecurityAuditEvent>(entry => entry.Outcome == SecurityAuditOutcome.Succeeded && entry.Changes != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static IdentityRole Role(string suffix)
    {
        string name = TenantRoleDefinitions.BuildRoleName(TenantId, suffix);
        return new IdentityRole { Id = suffix, Name = name, NormalizedName = name.ToUpperInvariant() };
    }

    private static IRequestContext OwnerContext()
    {
        Mock<IRequestContext> context = new();
        context.SetupGet(value => value.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantOwner))], "test")));
        context.SetupGet(value => value.TenantId).Returns(TenantId.ToString());
        context.SetupGet(value => value.UserId).Returns("owner");
        context.SetupGet(value => value.RequestId).Returns(Guid.NewGuid().ToString("N"));
        return context.Object;
    }
}
