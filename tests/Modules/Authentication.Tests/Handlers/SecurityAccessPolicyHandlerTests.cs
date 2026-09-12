using System.Security.Claims;
using FluentAssertions;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Queries;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class SecurityAccessPolicyHandlerTests
{
    [Fact]
    public async Task Authorized_read_preserves_the_policy_when_the_audit_store_is_unavailable()
    {
        SecurityAccessPolicyState expected = new(SecurityAccessPolicy.ProductEvaluated, 4);
        Mock<IAuthenticationSecurityPolicyStore> store = new();
        store.Setup(item => item.GetStateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(expected);
        Mock<ISecurityAuditWriter> audit = new();
        audit.Setup(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SecurityAuditStoreException("offline", new InvalidOperationException()));
        GetSecurityAccessPolicyHandler handler = new(store.Object, audit.Object, new TestRequestContext([IdentityDefinitions.PlatformAdmin]));

        SecurityAccessPolicyState result = await handler.Handle(new GetSecurityAccessPolicyQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        store.Verify(item => item.GetStateAsync(It.IsAny<CancellationToken>()), Times.Once);
        audit.Verify(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Unauthorized_change_is_rejected_and_audited()
    {
        Mock<IAuthenticationSecurityPolicyStore> store = new();
        RecordingAuditWriter audit = new();
        ChangeSecurityAccessPolicyHandler handler = new(store.Object, audit, new TestRequestContext([]), CreateSessionRegistry());

        Func<Task> act = () => handler.Handle(new ChangeSecurityAccessPolicyCommand(SecurityAccessPolicy.Default, 0), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        audit.Events.Select(item => item.Outcome).Should().ContainInOrder(SecurityAuditOutcome.Denied, SecurityAuditOutcome.Denied);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Authorized_change_saves_the_policy_and_records_completion()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.ProductEvaluated;
        Mock<IAuthenticationSecurityPolicyStore> store = new();
        store.Setup(item => item.SaveAsync(policy, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurityAccessPolicyState(policy, 1));
        RecordingAuditWriter audit = new();
        ChangeSecurityAccessPolicyHandler handler = new(store.Object, audit, new TestRequestContext([IdentityDefinitions.PlatformAdmin]), CreateSessionRegistry());

        SecurityAccessPolicyState result = await handler.Handle(new ChangeSecurityAccessPolicyCommand(policy, 0), CancellationToken.None);

        result.Policy.Should().Be(policy);
        result.Version.Should().Be(1);
        audit.Events.Select(item => item.Outcome).Should().ContainInOrder(SecurityAuditOutcome.Started, SecurityAuditOutcome.Succeeded);
        store.Verify(item => item.SaveAsync(policy, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Duplicate_started_audit_does_not_mutate_policy()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default;
        Mock<IAuthenticationSecurityPolicyStore> store = new();
        RecordingAuditWriter audit = new(SecurityAuditWriteResult.AlreadyRecorded);
        ChangeSecurityAccessPolicyHandler handler = new(store.Object, audit, new TestRequestContext([IdentityDefinitions.PlatformAdmin]), CreateSessionRegistry());

        Func<Task> act = () => handler.Handle(new ChangeSecurityAccessPolicyCommand(policy, 0), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        store.Verify(item => item.SaveAsync(It.IsAny<SecurityAccessPolicy>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Authorized_change_passes_the_client_version_and_audits_the_saved_version()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default;
        Mock<IAuthenticationSecurityPolicyStore> store = new();
        store.Setup(item => item.SaveAsync(policy, 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurityAccessPolicyState(policy, 8));
        RecordingAuditWriter audit = new();
        ChangeSecurityAccessPolicyHandler handler = new(store.Object, audit, new TestRequestContext([IdentityDefinitions.PlatformAdmin]), CreateSessionRegistry());

        SecurityAccessPolicyState result = await handler.Handle(new ChangeSecurityAccessPolicyCommand(policy, 7), CancellationToken.None);

        result.Version.Should().Be(8);
        audit.Events[^1].RedactedReason.Should().Be("version:8");
        store.Verify(item => item.SaveAsync(policy, 7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Concurrency_conflict_records_terminal_denied_audit_and_preserves_exception()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default;
        Mock<IAuthenticationSecurityPolicyStore> store = new();
        SecurityAccessPolicyConcurrencyException conflict = new("stale");
        store.Setup(item => item.SaveAsync(policy, 7, It.IsAny<CancellationToken>())).ThrowsAsync(conflict);
        RecordingAuditWriter audit = new();
        ChangeSecurityAccessPolicyHandler handler = new(store.Object, audit, new TestRequestContext([IdentityDefinitions.PlatformAdmin]), CreateSessionRegistry());

        Func<Task> act = () => handler.Handle(new ChangeSecurityAccessPolicyCommand(policy, 7), CancellationToken.None);

        await act.Should().ThrowAsync<SecurityAccessPolicyConcurrencyException>().Where(exception => ReferenceEquals(exception, conflict));
        audit.Events.Select(item => item.Outcome).Should().ContainInOrder(SecurityAuditOutcome.Started, SecurityAuditOutcome.Denied);
        audit.Events[^1].RedactedReason.Should().Be("concurrency_conflict");
    }

    [Fact]
    public async Task Validation_failure_records_terminal_denied_audit()
    {
        SecurityAccessPolicy invalid = SecurityAccessPolicy.Default with { FailedAttemptThreshold = 0 };
        Mock<IAuthenticationSecurityPolicyStore> store = new();
        RecordingAuditWriter audit = new();
        ChangeSecurityAccessPolicyHandler handler = new(store.Object, audit, new TestRequestContext([IdentityDefinitions.PlatformAdmin]), CreateSessionRegistry());

        Func<Task> act = () => handler.Handle(new ChangeSecurityAccessPolicyCommand(invalid, 0), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        audit.Events[^1].Outcome.Should().Be(SecurityAuditOutcome.Denied);
        audit.Events[^1].RedactedReason.Should().Be("validation_failed");
        store.Verify(item => item.SaveAsync(It.IsAny<SecurityAccessPolicy>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Completion_audit_failure_preserves_the_committed_policy_result()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.ProductEvaluated;
        Mock<IAuthenticationSecurityPolicyStore> store = new();
        store.Setup(item => item.SaveAsync(policy, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurityAccessPolicyState(policy, 1));
        RecordingAuditWriter audit = new(failNextUpdate: true);
        ChangeSecurityAccessPolicyHandler handler = new(store.Object, audit, new TestRequestContext([IdentityDefinitions.PlatformAdmin]), CreateSessionRegistry());

        SecurityAccessPolicyState result = await handler.Handle(new ChangeSecurityAccessPolicyCommand(policy, 0), CancellationToken.None);

        result.Version.Should().Be(1);
        audit.Events.Select(item => item.Outcome).Should().ContainInOrder(SecurityAuditOutcome.Started, SecurityAuditOutcome.Pending);
        store.Verify(item => item.SaveAsync(policy, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Retrying_the_same_operation_completes_the_durable_pending_outcome_without_repeating_the_save()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.ProductEvaluated;
        Mock<IAuthenticationSecurityPolicyStore> store = new();
        store.Setup(item => item.SaveAsync(policy, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurityAccessPolicyState(policy, 1));
        store.Setup(item => item.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurityAccessPolicyState(policy, 1));
        RecordingAuditWriter audit = new(failNextUpdate: true);
        TestRequestContext requestContext = new([IdentityDefinitions.PlatformAdmin]);
        ChangeSecurityAccessPolicyHandler handler = new(store.Object, audit, requestContext, CreateSessionRegistry());

        SecurityAccessPolicyState first = await handler.Handle(new ChangeSecurityAccessPolicyCommand(policy, 0), CancellationToken.None);
        SecurityAccessPolicyState result = await handler.Handle(new ChangeSecurityAccessPolicyCommand(policy, 0), CancellationToken.None);

        first.Version.Should().Be(1);
        result.Version.Should().Be(1);
        audit.Events.Select(item => item.Outcome).Should().ContainInOrder(SecurityAuditOutcome.Started, SecurityAuditOutcome.Succeeded);
        store.Verify(item => item.SaveAsync(policy, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task In_flight_duplicate_leaves_pending_audit_for_the_committing_attempt()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.ProductEvaluated;
        Mock<IAuthenticationSecurityPolicyStore> store = new();
        TaskCompletionSource saveEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseSave = new(TaskCreationOptions.RunContinuationsAsynchronously);
        store.Setup(item => item.SaveAsync(policy, 0, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                saveEntered.SetResult();
                await releaseSave.Task;
                return new SecurityAccessPolicyState(policy, 1);
            });
        store.Setup(item => item.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurityAccessPolicyState(SecurityAccessPolicy.Default, 0));
        RecordingAuditWriter audit = new();
        TestRequestContext requestContext = new([IdentityDefinitions.PlatformAdmin]);
        ChangeSecurityAccessPolicyHandler handler = new(store.Object, audit, requestContext, CreateSessionRegistry());

        Task<SecurityAccessPolicyState> original = handler.Handle(new ChangeSecurityAccessPolicyCommand(policy, 0), CancellationToken.None);
        await saveEntered.Task;
        Task duplicate = handler.Handle(new ChangeSecurityAccessPolicyCommand(policy, 0), CancellationToken.None);

        await ((Func<Task>)(async () => await duplicate)).Should().ThrowAsync<InvalidOperationException>();
        audit.Events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Pending);

        releaseSave.SetResult();
        SecurityAccessPolicyState result = await original;

        result.Version.Should().Be(1);
        audit.Events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Succeeded);
        audit.Events.Should().NotContain(item => item.Outcome == SecurityAuditOutcome.Denied);
    }

    [Fact]
    public async Task Duplicate_reconciliation_requires_a_confirmed_succeeded_audit_outcome()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.ProductEvaluated;
        Mock<IAuthenticationSecurityPolicyStore> store = new();
        store.Setup(item => item.GetStateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecurityAccessPolicyState(policy, 1));
        RecordingAuditWriter audit = new(SecurityAuditWriteResult.AlreadyRecorded);
        ChangeSecurityAccessPolicyHandler handler = new(store.Object, audit, new TestRequestContext([IdentityDefinitions.PlatformAdmin]), CreateSessionRegistry());

        Func<Task> act = () => handler.Handle(new ChangeSecurityAccessPolicyCommand(policy, 0), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        store.Verify(item => item.SaveAsync(It.IsAny<SecurityAccessPolicy>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class RecordingAuditWriter(
        SecurityAuditWriteResult initialStartedResult = SecurityAuditWriteResult.Recorded,
        bool failNextUpdate = false) : ISecurityAuditWriter, ISecurityAuditOutcomeUpdater
    {
        private readonly SecurityAuditWriteResult startedResult = initialStartedResult;
        private bool shouldFailNextUpdate = failNextUpdate;
        public List<SecurityAuditEvent> Events { get; } = [];

        public Task<string?> GetOutcomeAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult(Events.SingleOrDefault(item => item.IdempotencyKey == idempotencyKey)?.Outcome);

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            if (Events.Any(item => item.IdempotencyKey == auditEvent.IdempotencyKey))
                return Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
            Events.Add(auditEvent);
            return Task.FromResult(Events.Count == 1 ? startedResult : SecurityAuditWriteResult.Recorded);
        }

        public Task<SecurityAuditWriteResult> UpdateAsync(string tenantId, string idempotencyKey, string outcome, string? reason, CancellationToken cancellationToken)
        {
            SecurityAuditEvent? pending = Events.SingleOrDefault(item => item.IdempotencyKey == idempotencyKey);
            if (pending is null || pending.Outcome != SecurityAuditOutcome.Pending)
                return Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
            if (shouldFailNextUpdate)
            {
                shouldFailNextUpdate = false;
                throw new SecurityAuditStoreException("offline", new InvalidOperationException());
            }
            pending.Outcome = outcome;
            pending.RedactedReason = reason;
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }
    }

    private static IAuthenticationSessionRegistry CreateSessionRegistry()
    {
        Mock<IAuthenticationSessionRegistry> registry = new();
        registry.Setup(item => item.RevokeSessionsInvalidatedByPolicyAsync(
                It.IsAny<SecurityAccessPolicy>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return registry.Object;
    }

    private sealed class TestRequestContext(IReadOnlyList<string> roles) : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => "owner-1";
        public string? UserId => "admin-1";
        public string? TenantId => "tenant-a";
        public string? ResourceId => "authentication/security-access-policy";
        public string? TraceId => "trace-1";
        public string? RequestId => "request-1";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => new(new ClaimsIdentity(roles.Select(role => new Claim(ClaimTypes.Role, role)), "test"));
        public Request GetRequest() => new() { RequestId = RequestId!, Authority = Authority, TenantId = TenantId, UserId = UserId };
    }
}
