using System.Security.Claims;
using FluentAssertions;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Xunit;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class SecurityAuditChangePasswordTests
{
    [Fact]
    public async Task Audit_store_failure_preserves_the_password_mutation_result()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        updater.Setup(x => x.ChangePasswordAsync("user-42", "old", "Newpass1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordChangeOutcome.Success());
        FailingAuditWriter writer = new();
        ChangePasswordHandler handler = new(updater.Object, writer, new TestRequestContext(), new TestSecurityPolicyProvider());

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Success>().Which.Value.Should().BeTrue();
        updater.Verify(x => x.ChangePasswordAsync("user-42", "old", "Newpass1!", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Duplicate_request_does_not_repeat_the_password_mutation()
    {
        Mock<IUserAccountUpdater> updater = new();
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(x => x.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.AlreadyRecorded);
        ChangePasswordHandler handler = new(updater.Object, writer.Object, new TestRequestContext(), new TestSecurityPolicyProvider());

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.ProviderFailure);
        updater.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Successful_mutation_requires_a_completed_audit_record()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        updater.Setup(x => x.ChangePasswordAsync("user-42", "old", "Newpass1!", It.IsAny<CancellationToken>())).ReturnsAsync(PasswordChangeOutcome.Success());
        List<SecurityAuditEvent> events = [];
        RecordingWriter writer = new(events);
        ChangePasswordHandler handler = new(updater.Object, writer, new TestRequestContext(), new TestSecurityPolicyProvider());

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Success>().Which.Value.Should().BeTrue();
        events.Select(x => x.Outcome).Should().ContainInOrder(SecurityAuditOutcome.Started, SecurityAuditOutcome.Succeeded);
        updater.Verify(x => x.ChangePasswordAsync("user-42", "old", "Newpass1!", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Failed_mutation_writes_a_redacted_failure_outcome()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        updater.Setup(x => x.ChangePasswordAsync("user-42", "old", "Newpass1!", It.IsAny<CancellationToken>())).ReturnsAsync(PasswordChangeOutcome.Failure(PasswordChangeStatus.InvalidCurrentPassword));
        List<SecurityAuditEvent> events = [];
        RecordingWriter writer = new(events);
        ChangePasswordHandler handler = new(updater.Object, writer, new TestRequestContext(), new TestSecurityPolicyProvider());

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>().Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidCredentials);
        events[^1].Outcome.Should().Be(SecurityAuditOutcome.Failed);
        events[^1].RedactedReason.Should().Be("mutation_rejected");
        events[^1].Resource.Should().Be("user/user-42");
    }

    [Fact]
    public async Task Completion_store_failure_preserves_success_without_replaying_the_mutation()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        updater.Setup(x => x.ChangePasswordAsync("user-42", "old", "Newpass1!", It.IsAny<CancellationToken>())).ReturnsAsync(PasswordChangeOutcome.Success());
        FailingCompletionWriter writer = new();
        ChangePasswordHandler handler = new(updater.Object, writer, new TestRequestContext(), new TestSecurityPolicyProvider());

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Success>().Which.Value.Should().BeTrue();
        updater.Verify(x => x.ChangePasswordAsync("user-42", "old", "Newpass1!", It.IsAny<CancellationToken>()), Times.Once);
        writer.WriteCount.Should().Be(2);
    }

    [Fact]
    public async Task Retrying_after_completion_audit_outage_reconciles_without_replaying_the_password_mutation()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        updater.Setup(x => x.ChangePasswordAsync("user-42", "old", "Newpass1!", It.IsAny<CancellationToken>())).ReturnsAsync(PasswordChangeOutcome.Success());
        updater.Setup(x => x.PasswordMatchesAsync("user-42", "Newpass1!", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        RetryableCompletionWriter writer = new();
        ChangePasswordHandler handler = new(updater.Object, writer, new TestRequestContext(), new TestSecurityPolicyProvider());

        AuthenticationResult<bool> first = await handler.Handle(Command(), CancellationToken.None);
        AuthenticationResult<bool> retry = await handler.Handle(Command(), CancellationToken.None);

        first.Should().BeOfType<AuthenticationResult<bool>.Success>().Which.Value.Should().BeTrue();
        retry.Should().BeOfType<AuthenticationResult<bool>.Success>().Which.Value.Should().BeTrue();
        updater.Verify(x => x.ChangePasswordAsync("user-42", "old", "Newpass1!", It.IsAny<CancellationToken>()), Times.Once);
        writer.Events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Succeeded);
    }

    [Fact]
    public async Task Already_recorded_completion_requires_a_confirmed_succeeded_outcome()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        updater.Setup(x => x.ChangePasswordAsync("user-42", "old", "Newpass1!", It.IsAny<CancellationToken>())).ReturnsAsync(PasswordChangeOutcome.Success());
        AlreadyRecordedCompletionWriter writer = new();
        ChangePasswordHandler handler = new(updater.Object, writer, new TestRequestContext(), new TestSecurityPolicyProvider());

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.ProviderFailure);
        updater.Verify(x => x.ChangePasswordAsync("user-42", "old", "Newpass1!", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reusing_a_succeeded_idempotency_key_with_a_different_password_is_not_reported_as_success()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(x => x.PasswordMatchesAsync("user-42", "Newpass1!", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        ChangePasswordHandler handler = new(
            updater.Object,
            new SucceededReplayWriter(),
            new TestRequestContext(),
            new TestSecurityPolicyProvider());

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidInput);
        updater.Verify(x => x.PasswordMatchesAsync("user-42", "Newpass1!", It.IsAny<CancellationToken>()), Times.Once);
        updater.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Policy_rejection_is_reported_as_invalid_input_and_audited_as_policy_failure()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        updater.Setup(x => x.ChangePasswordAsync("user-42", "old", "Newpass1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordChangeOutcome.PolicyFailure([PasswordPolicyViolation.MinimumLength]));
        List<SecurityAuditEvent> events = [];
        RecordingWriter writer = new(events);
        ChangePasswordHandler handler = new(updater.Object, writer, new TestRequestContext(), new TestSecurityPolicyProvider());

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidInput);
        events[^1].Outcome.Should().Be(SecurityAuditOutcome.Failed);
        events[^1].RedactedReason.Should().Be("password_policy_rejected");
    }

    [Fact]
    public async Task Active_stronger_policy_rejects_a_password_before_mutation()
    {
        Mock<IUserAccountUpdater> updater = new();
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default with
        {
            PasswordPolicy = new PasswordPolicy(12, true, true, true, true)
        };
        ChangePasswordHandler handler = new(
            updater.Object,
            new RecordingWriter([]),
            new TestRequestContext(),
            new TestSecurityPolicyProvider(policy));

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidInput);
        updater.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Retry_of_a_previously_failed_operation_does_not_use_a_preexisting_matching_password_as_success()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(x => x.PasswordMatchesAsync("user-42", "Newpass1!", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        List<SecurityAuditEvent> events =
        [
            new() { IdempotencyKey = "operation-42:started", Outcome = SecurityAuditOutcome.Started },
            new() { IdempotencyKey = "operation-42:completed", Outcome = SecurityAuditOutcome.Failed }
        ];
        RecordingWriter writer = new(events);
        ChangePasswordHandler handler = new(updater.Object, writer, new TestRequestContext(), new TestSecurityPolicyProvider());

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.ProviderFailure);
        updater.Verify(x => x.PasswordMatchesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        updater.VerifyNoOtherCalls();
    }

    private static ChangePasswordCommand Command() => new("user-42", "old", "Newpass1!", "operation-42");

    private sealed class TestSecurityPolicyProvider(SecurityAccessPolicy? policy = null) : IAuthenticationSecurityPolicyProvider
    {
        public Task<SecurityAccessPolicy> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(policy ?? SecurityAccessPolicy.Default);
    }

    private sealed class RecordingWriter(List<SecurityAuditEvent> events) : ISecurityAuditWriter, ISecurityAuditOutcomeUpdater
    {
        public Task<string?> GetOutcomeAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult(events.SingleOrDefault(item => item.IdempotencyKey == idempotencyKey)?.Outcome);

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            if (events.Any(item => item.IdempotencyKey == auditEvent.IdempotencyKey))
                return Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
            events.Add(auditEvent);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }

        public Task<SecurityAuditWriteResult> UpdateAsync(string tenantId, string idempotencyKey, string outcome, string? reason, CancellationToken cancellationToken)
        {
            SecurityAuditEvent? pending = events.SingleOrDefault(item => item.IdempotencyKey == idempotencyKey);
            if (pending is null || pending.Outcome != SecurityAuditOutcome.Pending)
                return Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
            pending.Outcome = outcome;
            pending.RedactedReason = reason;
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }
    }

    private sealed class FailingCompletionWriter : ISecurityAuditWriter, ISecurityAuditOutcomeUpdater
    {
        private readonly List<SecurityAuditEvent> events = [];
        public int WriteCount { get; private set; }

        public Task<string?> GetOutcomeAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            WriteCount++;
            events.Add(auditEvent);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }

        public Task<SecurityAuditWriteResult> UpdateAsync(string tenantId, string idempotencyKey, string outcome, string? reason, CancellationToken cancellationToken) =>
            throw new SecurityAuditStoreException("offline", new InvalidOperationException());
    }

    private sealed class FailingAuditWriter : ISecurityAuditWriter, ISecurityAuditOutcomeUpdater
    {
        public Task<string?> GetOutcomeAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(null);

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken) =>
            throw new SecurityAuditStoreException("offline", new InvalidOperationException());

        public Task<SecurityAuditWriteResult> UpdateAsync(string tenantId, string idempotencyKey, string outcome, string? reason, CancellationToken cancellationToken) =>
            throw new SecurityAuditStoreException("offline", new InvalidOperationException());
    }

    private sealed class RetryableCompletionWriter : ISecurityAuditWriter, ISecurityAuditOutcomeUpdater
    {
        public List<SecurityAuditEvent> Events { get; } = [];
        private bool failCompletion = true;

        public Task<string?> GetOutcomeAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult(Events.SingleOrDefault(item => item.IdempotencyKey == idempotencyKey)?.Outcome);

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            if (Events.Any(item => item.IdempotencyKey == auditEvent.IdempotencyKey))
                return Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
            Events.Add(auditEvent);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }

        public Task<SecurityAuditWriteResult> UpdateAsync(string tenantId, string idempotencyKey, string outcome, string? reason, CancellationToken cancellationToken)
        {
            if (failCompletion && outcome == SecurityAuditOutcome.Succeeded)
            {
                failCompletion = false;
                throw new SecurityAuditStoreException("offline", new InvalidOperationException());
            }

            SecurityAuditEvent? pending = Events.SingleOrDefault(item => item.IdempotencyKey == idempotencyKey);
            if (pending is null || pending.Outcome != SecurityAuditOutcome.Pending)
                return Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
            pending.Outcome = outcome;
            pending.RedactedReason = reason;
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }
    }

    private sealed class AlreadyRecordedCompletionWriter : ISecurityAuditWriter, ISecurityAuditOutcomeUpdater
    {
        private readonly List<SecurityAuditEvent> events = [];

        public Task<string?> GetOutcomeAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult(events.SingleOrDefault(item => item.IdempotencyKey == idempotencyKey)?.Outcome);

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            events.Add(auditEvent);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }

        public Task<SecurityAuditWriteResult> UpdateAsync(string tenantId, string idempotencyKey, string outcome, string? reason, CancellationToken cancellationToken) =>
            Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
    }

    private sealed class SucceededReplayWriter : ISecurityAuditWriter, ISecurityAuditOutcomeUpdater
    {
        public Task<string?> GetOutcomeAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult<string?>(SecurityAuditOutcome.Succeeded);

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken) =>
            Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);

        public Task<SecurityAuditWriteResult> UpdateAsync(string tenantId, string idempotencyKey, string outcome, string? reason, CancellationToken cancellationToken) =>
            Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
    }

    private sealed class TestRequestContext : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => "owner-1";
        public string? UserId => "user-42";
        public string? TenantId => "tenant-a";
        public string? ResourceId => "account/password";
        public string? TraceId => "trace-42";
        public string? RequestId => "request-42";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-42")], "test"));
        public Request GetRequest() => new() { RequestId = RequestId!, Authority = Authority, TenantId = TenantId, UserId = UserId };
    }
}
