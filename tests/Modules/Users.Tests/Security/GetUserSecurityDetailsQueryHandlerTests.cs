using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Tests.Security;

public sealed class GetUserSecurityDetailsQueryHandlerTests
{
    private const string UserId = "user-42";
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Returns_user_roles_and_separately_paged_security_and_authentication_history()
    {
        Mock<IUsersService> users = AuthorizedUsers();
        Mock<ISecurityAuditQueryService> audit = new();
        SecurityAuditEventReadDto securityEvent = AuditEvent(SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent, SecurityAuditEventCatalog.SecurityAttributeCategory);
        SecurityAuditEventReadDto authenticationEvent = AuditEvent(SecurityAuditEventCatalog.AuthenticationPasskeyLoginEvent, SecurityAuditEventCatalog.AuthenticationCategory);
        audit.Setup(value => value.QueryPlatformAsync(
                It.Is<SecurityAuditPlatformQuery>(query =>
                    query.Query.SubjectId == UserId && query.Query.Page == 2 && query.Query.PageSize == 10 && query.Query.EventCategory == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(securityEvent));
        audit.Setup(value => value.QueryPlatformAsync(
                It.Is<SecurityAuditPlatformQuery>(query =>
                    query.Query.SubjectId == UserId && query.Query.Page == 2 && query.Query.PageSize == 10 &&
                    query.Query.EventCategory == SecurityAuditEventCatalog.AuthenticationCategory),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(authenticationEvent));
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, audit.Object);

        UserSecurityDetailsQueryResult result = await handler.Handle(new(UserId, 2, 10), CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
        result.User.Should().Match<User>(user => user.Id == UserId && user.Email == "user@example.test");
        result.AssignedRoles.Should().Contain([IdentityDefinitions.PlatformAdmin, TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantUser)]);
        result.SecurityHistoryAvailable.Should().BeTrue();
        result.SecurityHistory!.Items.Single().ClientInterfaceHost.Should().Be("admin.example.test");
        result.SecurityHistory.Items.Single().IsAuthenticationEvent.Should().BeFalse();
        result.AuthenticationHistoryAvailable.Should().BeTrue();
        result.AuthenticationHistory!.Items.Single().IsAuthenticationEvent.Should().BeTrue();
        audit.Verify(value => value.QueryPlatformAsync(It.IsAny<SecurityAuditPlatformQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Authentication_history_uses_its_own_page_when_given()
    {
        Mock<IUsersService> users = AuthorizedUsers();
        Mock<ISecurityAuditQueryService> audit = new();
        SecurityAuditEventReadDto securityEvent = AuditEvent(SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent, SecurityAuditEventCatalog.SecurityAttributeCategory);
        SecurityAuditEventReadDto authenticationEvent = AuditEvent(SecurityAuditEventCatalog.AuthenticationPasskeyLoginEvent, SecurityAuditEventCatalog.AuthenticationCategory);
        audit.Setup(value => value.QueryPlatformAsync(
                It.Is<SecurityAuditPlatformQuery>(query =>
                    query.Query.SubjectId == UserId && query.Query.Page == 1 && query.Query.PageSize == 10 && query.Query.EventCategory == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(securityEvent));
        audit.Setup(value => value.QueryPlatformAsync(
                It.Is<SecurityAuditPlatformQuery>(query =>
                    query.Query.SubjectId == UserId && query.Query.Page == 3 && query.Query.PageSize == 10 &&
                    query.Query.EventCategory == SecurityAuditEventCatalog.AuthenticationCategory),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(authenticationEvent));
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, audit.Object);

        UserSecurityDetailsQueryResult result = await handler.Handle(new(UserId, 1, 10, 3), CancellationToken.None);

        result.SecurityHistoryAvailable.Should().BeTrue();
        result.AuthenticationHistoryAvailable.Should().BeTrue();
        audit.Verify(value => value.QueryPlatformAsync(
            It.Is<SecurityAuditPlatformQuery>(query => query.Query.SubjectId == UserId && query.Query.Page == 1 && query.Query.EventCategory == null),
            It.IsAny<CancellationToken>()), Times.Once);
        audit.Verify(value => value.QueryPlatformAsync(
            It.Is<SecurityAuditPlatformQuery>(query => query.Query.SubjectId == UserId && query.Query.Page == 3 && query.Query.EventCategory == SecurityAuditEventCatalog.AuthenticationCategory),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Authentication_history_defaults_to_the_security_page()
    {
        Mock<IUsersService> users = AuthorizedUsers();
        Mock<ISecurityAuditQueryService> audit = new();
        SecurityAuditEventReadDto securityEvent = AuditEvent(SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent, SecurityAuditEventCatalog.SecurityAttributeCategory);
        SecurityAuditEventReadDto authenticationEvent = AuditEvent(SecurityAuditEventCatalog.AuthenticationPasskeyLoginEvent, SecurityAuditEventCatalog.AuthenticationCategory);
        audit.Setup(value => value.QueryPlatformAsync(
                It.Is<SecurityAuditPlatformQuery>(query =>
                    query.Query.SubjectId == UserId && query.Query.Page == 2 && query.Query.PageSize == 10 && query.Query.EventCategory == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(securityEvent));
        audit.Setup(value => value.QueryPlatformAsync(
                It.Is<SecurityAuditPlatformQuery>(query =>
                    query.Query.SubjectId == UserId && query.Query.Page == 2 && query.Query.PageSize == 10 &&
                    query.Query.EventCategory == SecurityAuditEventCatalog.AuthenticationCategory),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(authenticationEvent));
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, audit.Object);

        UserSecurityDetailsQueryResult result = await handler.Handle(new(UserId, 2, 10), CancellationToken.None);

        result.SecurityHistoryAvailable.Should().BeTrue();
        result.AuthenticationHistoryAvailable.Should().BeTrue();
        audit.Verify(value => value.QueryPlatformAsync(
            It.Is<SecurityAuditPlatformQuery>(query => query.Query.SubjectId == UserId && query.Query.Page == 2 && query.Query.EventCategory == null),
            It.IsAny<CancellationToken>()), Times.Once);
        audit.Verify(value => value.QueryPlatformAsync(
            It.Is<SecurityAuditPlatformQuery>(query => query.Query.SubjectId == UserId && query.Query.Page == 2 && query.Query.EventCategory == SecurityAuditEventCatalog.AuthenticationCategory),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Rejects_authentication_page_below_one_the_same_way_as_the_security_page()
    {
        Mock<IUsersService> users = AuthorizedUsers();
        Mock<ISecurityAuditQueryService> audit = new();
        audit.Setup(value => value.QueryPlatformAsync(
                It.Is<SecurityAuditPlatformQuery>(query => query.Query.Page >= 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(AuditEvent(SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent, SecurityAuditEventCatalog.SecurityAttributeCategory)));
        audit.Setup(value => value.QueryPlatformAsync(
                It.Is<SecurityAuditPlatformQuery>(query => query.Query.Page < 1),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentOutOfRangeException("query"));
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, audit.Object);

        Func<Task> securityPageZero = () => handler.Handle(new(UserId, 0, 10), CancellationToken.None);
        Func<Task> authenticationPageZero = () => handler.Handle(new(UserId, 1, 10, 0), CancellationToken.None);

        await securityPageZero.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await authenticationPageZero.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Returns_identity_and_roles_with_history_unavailable_when_audit_store_cannot_be_read()
    {
        Mock<IUsersService> users = AuthorizedUsers();
        Mock<ISecurityAuditQueryService> audit = new();
        audit.Setup(value => value.QueryPlatformAsync(It.IsAny<SecurityAuditPlatformQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SecurityAuditStoreException("Audit unavailable.", new InvalidOperationException()));
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, audit.Object);

        UserSecurityDetailsQueryResult result = await handler.Handle(new(UserId, 1, 25), CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.AssignedRoles.Should().Contain(IdentityDefinitions.PlatformAdmin);
        result.SecurityHistoryAvailable.Should().BeFalse();
        result.AuthenticationHistoryAvailable.Should().BeFalse();
        audit.Verify(value => value.QueryPlatformAsync(It.IsAny<SecurityAuditPlatformQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Denies_non_administrator_without_loading_user_or_audit_history()
    {
        Mock<IUsersService> users = new();
        Mock<ISecurityAuditQueryService> audit = new();
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext("ordinary-user"), users.Object, audit.Object);

        UserSecurityDetailsQueryResult result = await handler.Handle(new(UserId, 1, 25), CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
        result.User.Should().BeNull();
        users.Verify(value => value.GetUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        audit.Verify(value => value.QueryPlatformAsync(It.IsAny<SecurityAuditPlatformQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IUsersService> AuthorizedUsers()
    {
        Mock<IUsersService> users = new();
        users.Setup(value => value.GetUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = UserId, UserName = "user@example.test", Email = "user@example.test" });
        users.Setup(value => value.GetUserRolesAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new IdentityRole(IdentityDefinitions.PlatformAdmin),
                new IdentityRole(TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantUser))
            ]);
        return users;
    }

    private static IRequestContext RequestContext(string role)
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(value => value.User).Returns(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test")));
        return requestContext.Object;
    }

    private static SecurityAuditPageReadDto Page(SecurityAuditEventReadDto item) => new([item], 2, 10, 21, false);

    private static SecurityAuditEventReadDto AuditEvent(string eventName, string category) => new()
    {
        EventName = eventName,
        EventCategory = category,
        Outcome = SecurityAuditOutcome.Succeeded,
        SubjectId = UserId,
        Operation = "test.operation",
        OccurredAt = new DateTime(2026, 9, 28, 7, 0, 0, DateTimeKind.Utc),
        ClientInterfaceHost = "admin.example.test"
    };
}
