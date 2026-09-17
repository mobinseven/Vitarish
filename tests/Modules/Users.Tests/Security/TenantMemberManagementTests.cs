using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Security;

public sealed class TenantMemberManagementTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherTenantId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task List_is_tenant_scoped_and_requires_the_current_tenant_owner()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        ListTenantMembersQueryHandler handler = new(dbContext, RequestContext(TenantId, IdentityDefinitions.PlatformAdmin));

        IReadOnlyList<TenantMemberSummary> members = await handler.Handle(new ListTenantMembersQuery(null, 50), CancellationToken.None);

        members.Select(member => member.Id).Should().Equal("owner", "staff");
        members.Select(member => member.RoleSuffix).Should().OnlyContain(role => new[] { IdentityDefinitions.TenantOwner, IdentityDefinitions.TenantUser }.Contains(role));

        handler = new ListTenantMembersQueryHandler(dbContext, RequestContext(OtherTenantId, TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantOwner)));
        (await handler.Handle(new ListTenantMembersQuery(null, 50), CancellationToken.None)).Should().BeEmpty();
    }

    [Fact]
    public async Task Provision_creates_a_passwordless_contact_account_and_duplicate_calls_are_idempotent()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        UserManager<User> userManager = CreateUserManager(dbContext);
        ProvisionTenantMemberCommandHandler handler = new(
            dbContext,
            userManager,
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            ActiveSessions(false).Object,
            AuditWriter().Object,
            RequestContext(TenantId, IdentityDefinitions.PlatformAdmin));
        ProvisionTenantMemberCommand request = new(null, "new@example.com", null, "New", "Member");

        ProvisionTenantMemberResult first = await handler.Handle(request, CancellationToken.None);
        ProvisionTenantMemberResult second = await handler.Handle(request, CancellationToken.None);

        first.Created.Should().BeTrue();
        first.AlreadyMember.Should().BeFalse();
        second.Created.Should().BeFalse();
        second.AlreadyMember.Should().BeTrue();
        (await dbContext.Users.CountAsync(user => user.NormalizedEmail == "NEW@EXAMPLE.COM")).Should().Be(1);
        (await dbContext.UserRoles.CountAsync(membership => membership.UserId == first.User!.Id)).Should().Be(1);
    }

    [Fact]
    public async Task Removal_revokes_staff_membership_but_rejects_owner_removal()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        RemoveTenantMemberCommandHandler handler = new(dbContext, ActiveSessions(false).Object, AuditWriter().Object, RequestContext(TenantId, IdentityDefinitions.PlatformAdmin));

        IdentityResult staffResult = await handler.Handle(new RemoveTenantMemberCommand("staff"), CancellationToken.None);
        IdentityResult ownerResult = await handler.Handle(new RemoveTenantMemberCommand("owner"), CancellationToken.None);

        staffResult.Succeeded.Should().BeTrue();
        (await dbContext.UserRoles.AnyAsync(membership => membership.UserId == "staff")).Should().BeFalse();
        ownerResult.Errors.Single().Code.Should().Be(UsersCommandErrorCodes.TenantOwnerRemovalRejected);
        (await dbContext.UserRoles.AnyAsync(membership => membership.UserId == "owner")).Should().BeTrue();
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

    private static Mock<IAuthenticationActiveSessionQuery> ActiveSessions(bool active)
    {
        Mock<IAuthenticationActiveSessionQuery> sessions = new();
        sessions.Setup(value => value.HasActiveSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(active);
        return sessions;
    }

    private static Mock<ISecurityAuditWriter> AuditWriter()
    {
        Mock<ISecurityAuditWriter> audit = new();
        audit.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAuditWriteResult.Recorded);
        return audit;
    }

    private static IRequestContext RequestContext(Guid tenantId, string role)
    {
        Mock<IRequestContext> context = new();
        context.SetupGet(value => value.User).Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test")));
        context.SetupGet(value => value.TenantId).Returns(tenantId.ToString());
        context.SetupGet(value => value.UserId).Returns("administrator");
        context.SetupGet(value => value.RequestId).Returns(Guid.NewGuid().ToString("N"));
        return context.Object;
    }

    private static async Task<TestUsersDbContext> CreateDbContextAsync()
    {
        DbContextOptions<UsersDbContext> options = new DbContextOptionsBuilder<UsersDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        TestUsersDbContext dbContext = new(options);
        dbContext.Users.AddRange(
            new User { Id = "owner", UserName = "owner", Email = "owner@example.com" },
            new User { Id = "staff", UserName = "staff", Email = "staff@example.com" },
            new User { Id = "other", UserName = "other", Email = "other@example.com" });
        IdentityRole ownerRole = new() { Id = "owner-role", Name = TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantOwner), NormalizedName = "OWNER" };
        IdentityRole userRole = new() { Id = "user-role", Name = TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantUser), NormalizedName = "USER" };
        IdentityRole otherRole = new() { Id = "other-role", Name = TenantRoleDefinitions.BuildRoleName(OtherTenantId, IdentityDefinitions.TenantUser), NormalizedName = "OTHER" };
        dbContext.Roles.AddRange(ownerRole, userRole, otherRole);
        dbContext.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = "owner", RoleId = ownerRole.Id },
            new IdentityUserRole<string> { UserId = "staff", RoleId = userRole.Id },
            new IdentityUserRole<string> { UserId = "other", RoleId = otherRole.Id });
        await dbContext.SaveChangesAsync();
        return dbContext;
    }

    private sealed class TestUsersDbContext(DbContextOptions<UsersDbContext> options) : UsersDbContext(options);
}
