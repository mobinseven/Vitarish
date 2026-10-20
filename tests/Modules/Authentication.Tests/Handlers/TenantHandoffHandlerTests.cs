using FluentAssertions;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class TenantHandoffHandlerTests
{
    private const string UserId = "user-1";
    private const string TargetHost = "rezaei.clinic.example.test";
    private static readonly DateTime FixedNow = new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Redeems_a_ticket_once_on_its_own_host_and_starts_a_session()
    {
        TestRig rig = new();

        string? token = await rig.IssueHandler.Handle(new IssueTenantHandoffCommand(UserId, TargetHost), CancellationToken.None);
        token.Should().NotBeNull();

        AuthenticationResult<LoginResult> firstRedeem = await rig.RedeemHandler.Handle(new RedeemTenantHandoffCommand(token!), CancellationToken.None);

        firstRedeem.Should().BeOfType<AuthenticationResult<LoginResult>.Success>()
            .Which.Value.UserId.Should().Be(UserId);
        rig.SignIn.Verify(service => service.SignInAsync(UserId, It.IsAny<CancellationToken>()), Times.Once);
        rig.AuditEvents.Should().Contain(entry =>
            entry.EventName == AuthenticationSecurityAuditEvents.TenantHandoffRedeem &&
            entry.Outcome == SecurityAuditOutcome.Succeeded &&
            entry.SubjectId == UserId);

        AuthenticationResult<LoginResult> secondRedeem = await rig.RedeemHandler.Handle(new RedeemTenantHandoffCommand(token!), CancellationToken.None);

        secondRedeem.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidCredential);
        rig.SignIn.Verify(service => service.SignInAsync(UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Issue_stores_hashed_token_normalized_lowercase_host_and_sixty_second_expiry()
    {
        TestRig rig = new();

        string? token = await rig.IssueHandler.Handle(
            new IssueTenantHandoffCommand(UserId, "  Rezaei.Clinic.Example.Test. "),
            CancellationToken.None);

        token.Should().NotBeNull();
        token!.Length.Should().Be(TenantHandoffTokens.TokenLength);
        TenantHandoffTokens.IsWellFormed(token).Should().BeTrue();

        TenantHandoffTicket stored = rig.Tickets.Tickets.Should().ContainSingle().Subject;
        stored.TokenHash.Should().Be(TenantHandoffTokens.Hash(token)).And.NotBe(token);
        stored.TargetHost.Should().Be(TargetHost);
        stored.UserId.Should().Be(UserId);
        stored.IssuedAt.Should().Be(FixedNow);
        stored.ExpiresAt.Should().Be(FixedNow.AddSeconds(60));
    }

    [Fact]
    public async Task Issue_returns_null_and_stores_nothing_when_request_context_user_differs_from_command_user()
    {
        TestRig rig = new() { RequestContextUserId = "other-user" };

        string? token = await rig.IssueHandler.Handle(new IssueTenantHandoffCommand(UserId, TargetHost), CancellationToken.None);

        token.Should().BeNull();
        rig.Tickets.Tickets.Should().BeEmpty();
        rig.AuditEvents.Should().ContainSingle(entry =>
            entry.EventName == AuthenticationSecurityAuditEvents.TenantHandoffIssue &&
            entry.Outcome == SecurityAuditOutcome.Denied);
    }

    [Fact]
    public async Task Issue_returns_null_and_stores_nothing_when_policy_does_not_permit_tenant_handoff()
    {
        TestRig rig = new() { Policy = PolicyWithoutTenantHandoff() };

        string? token = await rig.IssueHandler.Handle(new IssueTenantHandoffCommand(UserId, TargetHost), CancellationToken.None);

        token.Should().BeNull();
        rig.Tickets.Tickets.Should().BeEmpty();
        rig.AuditEvents.Should().ContainSingle(entry =>
            entry.EventName == AuthenticationSecurityAuditEvents.TenantHandoffIssue &&
            entry.Outcome == SecurityAuditOutcome.Denied);
    }

    [Fact]
    public async Task Redeem_refuses_when_policy_does_not_permit_tenant_handoff()
    {
        TestRig rig = new();
        string token = (await rig.IssueHandler.Handle(new IssueTenantHandoffCommand(UserId, TargetHost), CancellationToken.None))!;
        rig.AuditEvents.Clear();
        rig.Policy = PolicyWithoutTenantHandoff();

        AuthenticationResult<LoginResult> result = await rig.RedeemHandler.Handle(new RedeemTenantHandoffCommand(token), CancellationToken.None);

        AssertRefused(rig, result, AuthenticationFailureReason.NotAllowed);
    }

    [Fact]
    public async Task Redeem_refuses_malformed_token()
    {
        TestRig rig = new();

        AuthenticationResult<LoginResult> result = await rig.RedeemHandler.Handle(new RedeemTenantHandoffCommand("short"), CancellationToken.None);

        AssertRefused(rig, result, AuthenticationFailureReason.InvalidCredential);
    }

    [Fact]
    public async Task Redeem_refuses_unknown_token()
    {
        TestRig rig = new();
        string unknownToken = TenantHandoffTokens.Create();

        AuthenticationResult<LoginResult> result = await rig.RedeemHandler.Handle(new RedeemTenantHandoffCommand(unknownToken), CancellationToken.None);

        AssertRefused(rig, result, AuthenticationFailureReason.InvalidCredential);
    }

    [Fact]
    public async Task Redeem_refuses_expired_ticket()
    {
        TestRig rig = new();
        string token = (await rig.IssueHandler.Handle(new IssueTenantHandoffCommand(UserId, TargetHost), CancellationToken.None))!;
        rig.AuditEvents.Clear();
        rig.UtcNow = FixedNow.AddSeconds(61);

        AuthenticationResult<LoginResult> result = await rig.RedeemHandler.Handle(new RedeemTenantHandoffCommand(token), CancellationToken.None);

        AssertRefused(rig, result, AuthenticationFailureReason.InvalidCredential);
    }

    [Fact]
    public async Task Redeem_refuses_wrong_host()
    {
        TestRig rig = new();
        string token = (await rig.IssueHandler.Handle(new IssueTenantHandoffCommand(UserId, TargetHost), CancellationToken.None))!;
        rig.AuditEvents.Clear();
        rig.ClientInterfaceHost = "other.clinic.example.test";

        AuthenticationResult<LoginResult> result = await rig.RedeemHandler.Handle(new RedeemTenantHandoffCommand(token), CancellationToken.None);

        AssertRefused(rig, result, AuthenticationFailureReason.InvalidCredential);
    }

    [Fact]
    public async Task Redeem_refuses_when_client_interface_host_is_null()
    {
        TestRig rig = new();
        string token = (await rig.IssueHandler.Handle(new IssueTenantHandoffCommand(UserId, TargetHost), CancellationToken.None))!;
        rig.AuditEvents.Clear();
        rig.ClientInterfaceHost = null;

        AuthenticationResult<LoginResult> result = await rig.RedeemHandler.Handle(new RedeemTenantHandoffCommand(token), CancellationToken.None);

        AssertRefused(rig, result, AuthenticationFailureReason.InvalidCredential);
    }

    [Fact]
    public async Task Redeem_refuses_when_user_is_denied_by_attempt_tracker()
    {
        TestRig rig = new();
        string token = (await rig.IssueHandler.Handle(new IssueTenantHandoffCommand(UserId, TargetHost), CancellationToken.None))!;
        rig.AuditEvents.Clear();
        string subjectKey = rig.KeyFactory.Object.Subject(AuthenticationMethod.Password, UserId);
        rig.AttemptTracker
            .Setup(tracker => tracker.IsDeniedAsync(subjectKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        AuthenticationResult<LoginResult> result = await rig.RedeemHandler.Handle(new RedeemTenantHandoffCommand(token), CancellationToken.None);

        AssertRefused(rig, result, AuthenticationFailureReason.LockedOut);
    }

    [Fact]
    public async Task Redeem_refuses_when_store_cannot_mark_ticket_redeemed()
    {
        TestRig rig = new();
        string token = (await rig.IssueHandler.Handle(new IssueTenantHandoffCommand(UserId, TargetHost), CancellationToken.None))!;
        rig.AuditEvents.Clear();
        rig.Tickets.FailMarkRedeemed = true;

        AuthenticationResult<LoginResult> result = await rig.RedeemHandler.Handle(new RedeemTenantHandoffCommand(token), CancellationToken.None);

        AssertRefused(rig, result, AuthenticationFailureReason.InvalidCredential);
    }

    [Fact]
    public async Task Redeem_returns_sign_in_service_failure_reason_when_session_sign_in_fails()
    {
        TestRig rig = new();
        string token = (await rig.IssueHandler.Handle(new IssueTenantHandoffCommand(UserId, TargetHost), CancellationToken.None))!;
        rig.AuditEvents.Clear();
        rig.SignIn
            .Setup(service => service.SignInAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationSessionSignInResult.Failed(new AuthenticationFailure(AuthenticationFailureReason.NoAccess, "No access.")));

        AuthenticationResult<LoginResult> result = await rig.RedeemHandler.Handle(new RedeemTenantHandoffCommand(token), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NoAccess);
        rig.AuditEvents.Should().ContainSingle(entry =>
            entry.EventName == AuthenticationSecurityAuditEvents.TenantHandoffRedeem &&
            entry.Outcome == SecurityAuditOutcome.Denied &&
            entry.SubjectId == UserId);
    }

    private static void AssertRefused(TestRig rig, AuthenticationResult<LoginResult> result, AuthenticationFailureReason expectedReason)
    {
        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(expectedReason);
        rig.SignIn.Verify(service => service.SignInAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        rig.AuditEvents.Should().ContainSingle(entry =>
            entry.EventName == AuthenticationSecurityAuditEvents.TenantHandoffRedeem &&
            entry.Outcome == SecurityAuditOutcome.Denied);
    }

    private static SecurityAccessPolicy PolicyWithoutTenantHandoff() =>
        SecurityAccessPolicy.Default with
        {
            PermittedPreAuthenticationActions = SecurityAccessPolicy.Default.PermittedPreAuthenticationActions
                .Where(action => action != PreAuthenticationAction.TenantHandoff)
                .ToHashSet()
        };

    private sealed class TestRig
    {
        public InMemoryTicketStore Tickets { get; } = new();
        public SecurityAccessPolicy Policy { get; set; } = SecurityAccessPolicy.Default;
        public string? RequestContextUserId { get; set; } = UserId;
        public string? ClientInterfaceHost { get; set; } = TargetHost;
        public DateTime UtcNow { get; set; } = FixedNow;
        public List<SecurityAuditEvent> AuditEvents { get; } = [];
        public Mock<IAuthenticationSignInService> SignIn { get; } = new();
        public Mock<IAuthenticationAttemptTracker> AttemptTracker { get; } = new();
        public Mock<IAuthenticationAttemptKeyFactory> KeyFactory { get; } = new();
        public IssueTenantHandoffHandler IssueHandler { get; }
        public RedeemTenantHandoffHandler RedeemHandler { get; }

        public TestRig()
        {
            Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
            policyProvider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Policy);

            SignIn.Setup(service => service.SignInAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(AuthenticationSessionSignInResult.Success());

            Mock<IClientInterfaceHostAccessor> hostAccessor = new();
            hostAccessor.SetupGet(accessor => accessor.ClientInterfaceHost)
                .Returns(() => ClientInterfaceHost);

            KeyFactory.Setup(factory => factory.Subject(It.IsAny<AuthenticationMethod>(), It.IsAny<string>()))
                .Returns((AuthenticationMethod method, string userId) => $"subject:{method}:{userId}");

            Mock<IAuthenticationUtcClock> clock = new();
            clock.SetupGet(item => item.UtcNow).Returns(() => UtcNow);

            Mock<IRequestContext> requestContext = new();
            requestContext.SetupGet(context => context.UserId).Returns(() => RequestContextUserId);
            requestContext.SetupGet(context => context.TenantId).Returns("tenant-1");
            requestContext.SetupGet(context => context.RequestId).Returns("request-1");
            requestContext.SetupGet(context => context.TraceId).Returns("trace-1");

            Mock<ISecurityAuditWriter> auditWriter = new();
            auditWriter.Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
                .Callback<SecurityAuditEvent, CancellationToken>((entry, _) => AuditEvents.Add(entry))
                .ReturnsAsync(SecurityAuditWriteResult.Recorded);

            TenantHandoffAuditor auditor = new(auditWriter.Object, requestContext.Object, clock.Object);
            IssueHandler = new IssueTenantHandoffHandler(Tickets, policyProvider.Object, requestContext.Object, clock.Object, auditor);
            RedeemHandler = new RedeemTenantHandoffHandler(
                Tickets,
                policyProvider.Object,
                SignIn.Object,
                hostAccessor.Object,
                AttemptTracker.Object,
                KeyFactory.Object,
                clock.Object,
                auditor);
        }
    }

    private sealed class InMemoryTicketStore : ITenantHandoffTicketStore
    {
        public List<TenantHandoffTicket> Tickets { get; } = [];
        public bool FailMarkRedeemed { get; set; }

        public Task AddAsync(TenantHandoffTicket ticket, DateTime nowUtc, CancellationToken cancellationToken)
        {
            Tickets.Add(ticket);
            return Task.CompletedTask;
        }

        public Task<TenantHandoffTicket?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
            Task.FromResult(Tickets.SingleOrDefault(item => item.TokenHash == tokenHash));

        public Task<bool> TryMarkRedeemedAsync(TenantHandoffTicket ticket, DateTime nowUtc, CancellationToken cancellationToken)
        {
            if (FailMarkRedeemed || ticket.RedeemedAt is not null)
                return Task.FromResult(false);
            ticket.Redeem(nowUtc);
            return Task.FromResult(true);
        }
    }
}
