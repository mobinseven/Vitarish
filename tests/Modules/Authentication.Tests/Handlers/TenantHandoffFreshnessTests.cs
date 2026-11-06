using FluentAssertions;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Authentication.Tests.Security;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class TenantHandoffFreshnessTests
{
    private const string UserId = "user-1";
    private const string TargetHost = "clinic.example.test";
    private static readonly DateTime Now = new(2026, 11, 6, 8, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handoff_preserves_an_old_source_authentication_time_without_renewing_it()
    {
        DateTime staleAuthenticationTime = Now - RecentAuthenticationPolicy.MaximumAge - TimeSpan.FromSeconds(1);
        TestRig rig = new(staleAuthenticationTime);

        string token = (await rig.IssueHandler.Handle(new IssueTenantHandoffCommand(UserId, TargetHost), CancellationToken.None))!;
        TenantHandoffTicket storedTicket = rig.Tickets.Tickets.Should().ContainSingle().Subject;
        storedTicket.AuthenticatedAt.Should().Be(staleAuthenticationTime);

        AuthenticationResult<LoginResult> result = await rig.RedeemHandler.Handle(new RedeemTenantHandoffCommand(token), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Success>();
        rig.SignIn.Verify(service => service.SignInFromTenantHandoffAsync(
            UserId,
            staleAuthenticationTime,
            It.IsAny<CancellationToken>()), Times.Once);
        rig.SignIn.Verify(service => service.SignInAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handoff_preserves_a_missing_source_authentication_time_as_missing()
    {
        TestRig rig = new(authenticatedAt: null);

        string token = (await rig.IssueHandler.Handle(new IssueTenantHandoffCommand(UserId, TargetHost), CancellationToken.None))!;
        TenantHandoffTicket storedTicket = rig.Tickets.Tickets.Should().ContainSingle().Subject;
        storedTicket.AuthenticatedAt.Should().BeNull();

        AuthenticationResult<LoginResult> result = await rig.RedeemHandler.Handle(new RedeemTenantHandoffCommand(token), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Success>();
        rig.SignIn.Verify(service => service.SignInFromTenantHandoffAsync(
            UserId,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        rig.SignIn.Verify(service => service.SignInAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class TestRig
    {
        private DateTime UtcNow { get; } = Now;
        public InMemoryTicketStore Tickets { get; } = new();
        public Mock<IAuthenticationSignInService> SignIn { get; } = new();
        public IssueTenantHandoffHandler IssueHandler { get; }
        public RedeemTenantHandoffHandler RedeemHandler { get; }

        public TestRig(DateTime? authenticatedAt)
        {
            Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
            policyProvider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(SecurityAccessPolicy.Default);

            Mock<IRequestContext> requestContext = new();
            requestContext.SetupGet(context => context.UserId).Returns(UserId);
            requestContext.SetupGet(context => context.TenantId).Returns("tenant-1");
            requestContext.SetupGet(context => context.RequestId).Returns("request-1");
            requestContext.SetupGet(context => context.TraceId).Returns("trace-1");

            Mock<IAuthenticationUtcClock> clock = new();
            clock.SetupGet(item => item.UtcNow).Returns(() => UtcNow);

            Mock<ISecurityAuditWriter> auditWriter = new();
            auditWriter.Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(SecurityAuditWriteResult.Recorded);
            AuthenticationAttemptAuditor auditor = new(auditWriter.Object, requestContext.Object, clock.Object);

            Mock<IAuthenticationSessionRegistry> sessions = new();
            sessions.Setup(registry => registry.GetCurrentAuthenticationAtAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(authenticatedAt);
            IssueHandler = new IssueTenantHandoffHandler(
                Tickets,
                policyProvider.Object,
                requestContext.Object,
                clock.Object,
                auditor,
                SignIn.Object,
                sessions.Object);

            SignIn.Setup(service => service.SignInFromTenantHandoffAsync(
                    It.IsAny<string>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthenticationSessionSignInResult.Success());

            Mock<IClientInterfaceHostAccessor> hostAccessor = new();
            hostAccessor.SetupGet(accessor => accessor.ClientInterfaceHost).Returns(TargetHost);

            Mock<IAuthenticationAttemptTracker> attemptTracker = new();
            attemptTracker.Setup(tracker => tracker.IsDeniedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            Mock<IAuthenticationAttemptKeyFactory> attemptKeyFactory = new();
            attemptKeyFactory.Setup(factory => factory.SubjectCandidates(
                    It.IsAny<AuthenticationMethod>(),
                    It.IsAny<string>()))
                .Returns((AuthenticationMethod method, string userId) => [$"subject:{method}:{userId}"]);

            Mock<IAuthenticationUserDirectory> users = new();
            users.Setup(directory => directory.FindByIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuthenticationUserDescriptor(UserId, null, null, null, null, null, null, false));

            Mock<IAuthenticationRedirectResolver> redirects = new();
            redirects.Setup(resolver => resolver.ResolveAsync(
                    null,
                    false,
                    UserId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync("/home");

            RedeemHandler = new RedeemTenantHandoffHandler(
                Tickets,
                policyProvider.Object,
                SignIn.Object,
                hostAccessor.Object,
                attemptTracker.Object,
                attemptKeyFactory.Object,
                TestSourceAttemptGuards.WithoutClientAddress(),
                users.Object,
                redirects.Object,
                clock.Object,
                auditor);
        }
    }

    private sealed class InMemoryTicketStore : ITenantHandoffTicketStore
    {
        public List<TenantHandoffTicket> Tickets { get; } = [];

        public Task AddAsync(TenantHandoffTicket ticket, DateTime nowUtc, CancellationToken cancellationToken)
        {
            Tickets.Add(ticket);
            return Task.CompletedTask;
        }

        public Task<TenantHandoffTicket?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
            Task.FromResult(Tickets.SingleOrDefault(ticket => ticket.TokenHash == tokenHash));

        public Task<bool> TryMarkRedeemedAsync(TenantHandoffTicket ticket, DateTime nowUtc, CancellationToken cancellationToken)
        {
            if (ticket.RedeemedAt is not null)
                return Task.FromResult(false);
            ticket.Redeem(nowUtc);
            return Task.FromResult(true);
        }
    }
}
