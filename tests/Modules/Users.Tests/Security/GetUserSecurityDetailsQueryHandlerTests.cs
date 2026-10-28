using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private static readonly EnabledAuthenticationMethods EffectiveMethods = new(true, false, true, false);

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
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, Passkeys().Object, audit.Object, NullLogger<GetUserSecurityDetailsQueryHandler>.Instance, EnabledMethods());

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
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, Passkeys().Object, audit.Object, NullLogger<GetUserSecurityDetailsQueryHandler>.Instance, EnabledMethods());

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
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, Passkeys().Object, audit.Object, NullLogger<GetUserSecurityDetailsQueryHandler>.Instance, EnabledMethods());

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
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, Passkeys().Object, audit.Object, NullLogger<GetUserSecurityDetailsQueryHandler>.Instance, EnabledMethods());

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
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, Passkeys().Object, audit.Object, NullLogger<GetUserSecurityDetailsQueryHandler>.Instance, EnabledMethods());

        UserSecurityDetailsQueryResult result = await handler.Handle(new(UserId, 1, 25), CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.AssignedRoles.Should().Contain(IdentityDefinitions.PlatformAdmin);
        result.SecurityHistoryAvailable.Should().BeFalse();
        result.AuthenticationHistoryAvailable.Should().BeFalse();
        audit.Verify(value => value.QueryPlatformAsync(It.IsAny<SecurityAuditPlatformQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reports_which_authentication_data_the_user_holds_without_exposing_it()
    {
        Mock<IUsersService> users = new();
        users.Setup(value => value.GetUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User
            {
                Id = UserId,
                Email = "user@example.test",
                EmailConfirmed = true,
                PhoneNumber = "+989121234567",
                PhoneNumberConfirmed = false,
                PasswordHash = null
            });
        users.Setup(value => value.GetUserRolesAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        DateTime changedAt = new(2026, 10, 19, 9, 30, 0, DateTimeKind.Utc);
        users.Setup(value => value.GetUserPasswordStatusAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPasswordStatus(true, changedAt));
        DateTime registeredAt = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        DateTime lastUsedAt = new(2026, 10, 20, 9, 30, 0, DateTimeKind.Utc);
        Guid credentialId = Guid.NewGuid();
        Mock<IWebAuthnService> passkeys = Passkeys(new WebAuthnCredentialDto { Id = credentialId, FriendlyName = "Office laptop", RegisteredAt = registeredAt, LastUsedAt = lastUsedAt, Transports = "internal" });
        Mock<ISecurityAuditQueryService> audit = new();
        audit.Setup(value => value.QueryPlatformAsync(It.IsAny<SecurityAuditPlatformQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(AuditEvent(SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent, SecurityAuditEventCatalog.SecurityAttributeCategory)));
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, passkeys.Object, audit.Object, NullLogger<GetUserSecurityDetailsQueryHandler>.Instance, EnabledMethods());

        UserSecurityDetailsQueryResult result = await handler.Handle(new(UserId, 1, 25), CancellationToken.None);

        result.AuthenticationMethods.Should().BeEquivalentTo(new UserAuthenticationMethodsResult(
            PasswordSet: true,
            EmailVerified: true,
            PhoneNumberVerified: false,
            Passkeys: [new UserPasskeyEntry(credentialId, "Office laptop", registeredAt, lastUsedAt)],
            EnabledMethods: EffectiveMethods,
            PasswordChangedAt: changedAt));
        passkeys.Verify(value => value.ListCredentialsAsync(UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reports_authentication_methods_unavailable_when_the_passkey_lookup_fails()
    {
        Mock<IUsersService> users = AuthorizedUsers();
        Mock<IWebAuthnService> passkeys = new();
        passkeys.Setup(value => value.ListCredentialsAsync(UserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Passkey store unavailable."));
        Mock<ISecurityAuditQueryService> audit = new();
        audit.Setup(value => value.QueryPlatformAsync(It.IsAny<SecurityAuditPlatformQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(AuditEvent(SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent, SecurityAuditEventCatalog.SecurityAttributeCategory)));
        RecordingLogger<GetUserSecurityDetailsQueryHandler> logger = new();
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, passkeys.Object, audit.Object, logger, EnabledMethods());

        UserSecurityDetailsQueryResult result = await handler.Handle(new(UserId, 1, 25), CancellationToken.None);

        result.IsAuthorized.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.AuthenticationMethods.Should().BeNull();
        result.AuthenticationMethodsUnavailable.Should().BeTrue();
        result.SecurityHistoryAvailable.Should().BeTrue();
        logger.ErrorCount.Should().Be(1);
    }

    [Fact]
    public async Task Does_not_load_authentication_methods_for_history_page_changes()
    {
        Mock<IUsersService> users = AuthorizedUsers();
        Mock<IWebAuthnService> passkeys = Passkeys();
        Mock<ISecurityAuditQueryService> audit = new();
        audit.Setup(value => value.QueryPlatformAsync(It.IsAny<SecurityAuditPlatformQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Page(AuditEvent(SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent, SecurityAuditEventCatalog.SecurityAttributeCategory)));
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, passkeys.Object, audit.Object, NullLogger<GetUserSecurityDetailsQueryHandler>.Instance, EnabledMethods());

        UserSecurityDetailsQueryResult result = await handler.Handle(new(UserId, 2, 25, 3), CancellationToken.None);

        result.SecurityHistoryAvailable.Should().BeTrue();
        result.AuthenticationMethods.Should().BeNull();
        result.AuthenticationMethodsUnavailable.Should().BeFalse();
        passkeys.Verify(value => value.ListCredentialsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Propagates_cancellation_from_the_passkey_lookup()
    {
        Mock<IUsersService> users = AuthorizedUsers();
        Mock<IWebAuthnService> passkeys = new();
        passkeys.Setup(value => value.ListCredentialsAsync(UserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, passkeys.Object, Mock.Of<ISecurityAuditQueryService>(), NullLogger<GetUserSecurityDetailsQueryHandler>.Instance, EnabledMethods());

        Func<Task> load = () => handler.Handle(new(UserId, 1, 25), CancellationToken.None);

        await load.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Does_not_report_a_confirmation_flag_without_the_contact_or_a_password_without_a_hash()
    {
        Mock<IUsersService> users = new();
        users.Setup(value => value.GetUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = UserId, Email = null, EmailConfirmed = true, PhoneNumber = null, PhoneNumberConfirmed = true, PasswordHash = null });
        users.Setup(value => value.GetUserRolesAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        users.Setup(value => value.GetUserPasswordStatusAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPasswordStatus(false, null));
        Mock<ISecurityAuditQueryService> audit = new();
        audit.Setup(value => value.QueryPlatformAsync(It.IsAny<SecurityAuditPlatformQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SecurityAuditStoreException("Audit unavailable.", new InvalidOperationException()));
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext(IdentityDefinitions.PlatformAdmin), users.Object, Passkeys().Object, audit.Object, NullLogger<GetUserSecurityDetailsQueryHandler>.Instance, EnabledMethods());

        UserSecurityDetailsQueryResult result = await handler.Handle(new(UserId, 1, 25), CancellationToken.None);

        result.SecurityHistoryAvailable.Should().BeFalse();
        result.AuthenticationMethods.Should().BeEquivalentTo(new UserAuthenticationMethodsResult(false, false, false, [], EffectiveMethods));
    }

    [Fact]
    public async Task Denies_non_administrator_without_loading_user_or_audit_history()
    {
        Mock<IUsersService> users = new();
        Mock<ISecurityAuditQueryService> audit = new();
        Mock<IWebAuthnService> passkeys = Passkeys();
        GetUserSecurityDetailsQueryHandler handler = new(RequestContext("ordinary-user"), users.Object, passkeys.Object, audit.Object, NullLogger<GetUserSecurityDetailsQueryHandler>.Instance, EnabledMethods());

        UserSecurityDetailsQueryResult result = await handler.Handle(new(UserId, 1, 25), CancellationToken.None);

        result.IsAuthorized.Should().BeFalse();
        result.User.Should().BeNull();
        users.Verify(value => value.GetUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        audit.Verify(value => value.QueryPlatformAsync(It.IsAny<SecurityAuditPlatformQuery>(), It.IsAny<CancellationToken>()), Times.Never);
        passkeys.Verify(value => value.ListCredentialsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IUsersService> AuthorizedUsers()
    {
        Mock<IUsersService> users = new();
        users.Setup(value => value.GetUserAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = UserId, UserName = "user@example.test", Email = "user@example.test" });
        users.Setup(value => value.GetUserPasswordStatusAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPasswordStatus(false, null));
        users.Setup(value => value.GetUserRolesAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new IdentityRole(IdentityDefinitions.PlatformAdmin),
                new IdentityRole(TenantRoleDefinitions.BuildRoleName(TenantId, IdentityDefinitions.TenantUser))
            ]);
        return users;
    }

    private static IEnabledAuthenticationMethods EnabledMethods()
    {
        Mock<IEnabledAuthenticationMethods> methods = new();
        methods.Setup(value => value.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(EffectiveMethods);
        return methods.Object;
    }

    private static Mock<IWebAuthnService> Passkeys(params WebAuthnCredentialDto[] credentials)
    {
        Mock<IWebAuthnService> passkeys = new();
        passkeys.Setup(value => value.ListCredentialsAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(credentials);
        return passkeys;
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

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public int ErrorCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Error)
                ErrorCount++;
        }
    }
}
