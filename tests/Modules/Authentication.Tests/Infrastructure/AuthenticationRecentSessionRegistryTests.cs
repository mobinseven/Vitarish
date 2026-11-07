using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;

namespace Authentication.Tests.Infrastructure;

public sealed class AuthenticationRecentSessionRegistryTests
{
    [Fact]
    public async Task Authenticated_bearer_without_registry_session_reference_is_not_recent_authentication()
    {
        await using AuthenticationDbContext db = CreateDatabase();
        ClaimsPrincipal principal = new(new ClaimsIdentity([], ModuleEndpointAuthenticationSchemes.Bearer));
        AuthenticationSessionRegistry registry = CreateRegistry(db, principal);

        DateTime? authenticatedAt = await registry.GetCurrentAuthenticationAtAsync(UserId, CancellationToken.None);

        authenticatedAt.Should().BeNull();
    }

    [Fact]
    public async Task Registry_timestamp_is_returned_only_for_the_current_users_active_session_and_tenant()
    {
        await using AuthenticationDbContext db = CreateDatabase();
        Guid sessionId = Guid.NewGuid();
        DateTime authenticatedAt = new(2026, 11, 6, 8, 30, 0, DateTimeKind.Utc);
        ClaimsPrincipal principal = new(new ClaimsIdentity(
            [new Claim(AuthenticationSessionClaimTypes.SessionId, sessionId.ToString())],
            ModuleEndpointAuthenticationSchemes.Browser));
        AuthenticationSessionRegistry registry = CreateRegistry(db, principal);
        db.AuthenticationSessions.Add(new AuthenticationSession
        {
            Id = sessionId,
            UserId = UserId,
            TenantId = TenantId,
            AuthenticatedAt = authenticatedAt,
            CreatedAt = authenticatedAt,
            LastActivityAt = authenticatedAt,
            ClientIpAddress = "127.0.0.1"
        });
        await db.SaveChangesAsync();

        (await registry.GetCurrentAuthenticationAtAsync(UserId, CancellationToken.None)).Should().Be(authenticatedAt);

        AuthenticationSession stored = await db.AuthenticationSessions.SingleAsync();
        stored.TerminatedAt = authenticatedAt.AddMinutes(1);
        await db.SaveChangesAsync();

        (await registry.GetCurrentAuthenticationAtAsync(UserId, CancellationToken.None)).Should().BeNull();
    }

    private static AuthenticationDbContext CreateDatabase()
    {
        DbContextOptions<AuthenticationDbContext> options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AuthenticationDbContext(options);
    }

    private static AuthenticationSessionRegistry CreateRegistry(AuthenticationDbContext db, ClaimsPrincipal principal)
    {
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        Mock<IClientIpAddressAccessor> clientIp = new();
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.User).Returns(principal);
        requestContext.SetupGet(context => context.UserId).Returns(UserId);
        requestContext.SetupGet(context => context.TenantId).Returns(TenantId);
        return new AuthenticationSessionRegistry(
            db,
            policyProvider.Object,
            clientIp.Object,
            requestContext.Object,
            Mock.Of<ISecurityAuditWriter>(),
            TimeProvider.System);
    }

    private const string UserId = "user-1";
    private const string TenantId = "tenant-1";
}
