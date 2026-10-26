using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MediatR;
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
using Vitarish.Users.Application.Security;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Infrastructure.Service;

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
    public async Task List_by_user_ids_returns_only_those_members_of_the_current_tenant()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        ListTenantMembersQueryHandler handler = new(dbContext, RequestContext(TenantId, IdentityDefinitions.PlatformAdmin));

        IReadOnlyList<TenantMemberSummary> members = await handler.Handle(
            new ListTenantMembersQuery(null, 3, ["staff", "other", "staf"]),
            CancellationToken.None);

        members.Select(member => member.Id).Should().Equal("staff");
    }

    [Fact]
    public async Task List_by_an_empty_user_id_set_returns_no_members()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        ListTenantMembersQueryHandler handler = new(dbContext, RequestContext(TenantId, IdentityDefinitions.PlatformAdmin));

        IReadOnlyList<TenantMemberSummary> members = await handler.Handle(new ListTenantMembersQuery(null, 1, []), CancellationToken.None);

        members.Should().BeEmpty();
    }

    [Fact]
    public void List_rejects_more_user_ids_than_the_result_cap()
        => new ListTenantMembersQueryValidator().Validate(new ListTenantMembersQuery(null, 1, ["owner", "staff"])).IsValid.Should().BeFalse();

    [Fact]
    public async Task Provision_creates_a_passwordless_contact_account_and_duplicate_calls_are_idempotent()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        UserManager<User> userManager = CreateUserManager(dbContext);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        IRequestContext requestContext = RequestContext(TenantId, IdentityDefinitions.PlatformAdmin);
        ProvisionTenantMemberCommandHandler handler = new(
            dbContext,
            userManager,
            CreateSender(dbContext),
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            ActiveSessions(false).Object,
            auditWriter.Object,
            requestContext,
            new UsersManagementAudit(auditWriter.Object, requestContext, TimeProvider.System));
        ProvisionTenantMemberCommand request = new(null, "new@example.com", null, "New", "Member");

        ProvisionTenantMemberResult first = await handler.Handle(request, CancellationToken.None);
        ProvisionTenantMemberResult second = await handler.Handle(request, CancellationToken.None);

        first.Created.Should().BeTrue();
        first.AlreadyMember.Should().BeFalse();
        second.Created.Should().BeFalse();
        second.AlreadyMember.Should().BeTrue();
        (await dbContext.Users.CountAsync(user => user.NormalizedEmail == "NEW@EXAMPLE.COM")).Should().Be(1);
        (await dbContext.UserRoles.CountAsync(membership => membership.UserId == first.User!.Id)).Should().Be(1);
        auditWriter.Verify(writer => writer.WriteAsync(
            It.Is<SecurityAuditEvent>(entry =>
                entry.EventName == SecurityAuditEventCatalog.UserCreationEvent &&
                entry.SubjectId == first.User!.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        SecurityAuditEvent[] events = AuditEvents(auditWriter);
        events.Single(entry => entry.EventName == SecurityAuditEventCatalog.UserCreationEvent).Changes.Should().Equal(
            new SecurityAuditChange(SecurityAuditChangeFields.Email, null, "new@example.com"),
            new SecurityAuditChange(SecurityAuditChangeFields.FirstName, null, "New"),
            new SecurityAuditChange(SecurityAuditChangeFields.LastName, null, "Member"));
        events.Single(entry =>
            entry.EventName == SecurityAuditEventCatalog.TenantMemberProvisioningEvent &&
            entry.Outcome == SecurityAuditOutcome.Succeeded &&
            entry.Changes is not null).Changes.Should().Equal(
            new SecurityAuditChange(
                SecurityAuditChangeFields.TenantRole,
                SecurityAuditReasonCatalog.TenantRoleStateNone,
                SecurityAuditReasonCatalog.TenantRoleStateUser));
    }

    [Fact]
    public async Task Provision_with_a_local_format_phone_stores_the_e164_number()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        UserManager<User> userManager = CreateUserManager(dbContext);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        IRequestContext requestContext = RequestContext(TenantId, IdentityDefinitions.PlatformAdmin);
        ProvisionTenantMemberCommandHandler handler = new(
            dbContext,
            userManager,
            CreateSender(dbContext),
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            ActiveSessions(false).Object,
            auditWriter.Object,
            requestContext,
            new UsersManagementAudit(auditWriter.Object, requestContext, TimeProvider.System));

        ProvisionTenantMemberResult result = await handler.Handle(
            new(null, null, "09123456789", "New", "Member"), CancellationToken.None);

        result.Created.Should().BeTrue();
        User created = await dbContext.Users.AsNoTracking().SingleAsync(user => user.Id == result.User!.Id);
        created.PhoneNumber.Should().Be("+989123456789");
        created.UserName.Should().Be("+989123456789");
    }

    [Fact]
    public async Task Provision_rejects_a_contact_already_owned_by_another_account()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        dbContext.Users.Add(new User { Id = "username-owner", UserName = "taken@example.test", NormalizedUserName = "TAKEN@EXAMPLE.TEST" });
        await dbContext.SaveChangesAsync();
        UserManager<User> userManager = CreateUserManager(dbContext);
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        IRequestContext requestContext = RequestContext(TenantId, IdentityDefinitions.PlatformAdmin);
        ProvisionTenantMemberCommandHandler handler = new(
            dbContext,
            userManager,
            CreateSender(dbContext),
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            ActiveSessions(false).Object,
            auditWriter.Object,
            requestContext,
            new UsersManagementAudit(auditWriter.Object, requestContext, TimeProvider.System));

        ProvisionTenantMemberResult result = await handler.Handle(
            new(null, "TAKEN@example.test", null, "New", "Member"), CancellationToken.None);

        result.Created.Should().BeFalse();
        result.ErrorCode.Should().Be(UsersCommandErrorCodes.ContactIdentifierInUse);
        (await dbContext.Users.CountAsync()).Should().Be(4);
        auditWriter.Verify(writer => writer.WriteAsync(
            It.Is<SecurityAuditEvent>(entry =>
                entry.EventName == SecurityAuditEventCatalog.UserCreationFailureEvent &&
                entry.Outcome == SecurityAuditOutcome.Denied &&
                entry.RedactedReason == SecurityAuditReasonCatalog.MutationRejected),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Removal_revokes_staff_membership_but_rejects_owner_removal()
    {
        await using TestUsersDbContext dbContext = await CreateDbContextAsync();
        Mock<ISecurityAuditWriter> auditWriter = AuditWriter();
        RemoveTenantMemberCommandHandler handler = new(dbContext, ActiveSessions(false).Object, auditWriter.Object, RequestContext(TenantId, IdentityDefinitions.PlatformAdmin));

        IdentityResult staffResult = await handler.Handle(new RemoveTenantMemberCommand("staff"), CancellationToken.None);
        IdentityResult ownerResult = await handler.Handle(new RemoveTenantMemberCommand("owner"), CancellationToken.None);

        staffResult.Succeeded.Should().BeTrue();
        (await dbContext.UserRoles.AnyAsync(membership => membership.UserId == "staff")).Should().BeFalse();
        ownerResult.Errors.Single().Code.Should().Be(UsersCommandErrorCodes.TenantOwnerRemovalRejected);
        (await dbContext.UserRoles.AnyAsync(membership => membership.UserId == "owner")).Should().BeTrue();
        AuditEvents(auditWriter).Single(entry =>
            entry.EventName == SecurityAuditEventCatalog.TenantMemberRemovalEvent &&
            entry.Outcome == SecurityAuditOutcome.Succeeded).Changes.Should().Equal(
            new SecurityAuditChange(
                SecurityAuditChangeFields.TenantRole,
                SecurityAuditReasonCatalog.TenantRoleStateUser,
                SecurityAuditReasonCatalog.TenantRoleStateNone));
    }

    private static ISender CreateSender(UsersDbContext dbContext)
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

    private static SecurityAuditEvent[] AuditEvents(Mock<ISecurityAuditWriter> auditWriter) => auditWriter.Invocations
        .Select(invocation => (SecurityAuditEvent)invocation.Arguments[0])
        .ToArray();

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
