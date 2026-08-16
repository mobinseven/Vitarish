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
    public async Task Audit_store_failure_prevents_the_password_mutation()
    {
        Mock<IUserAccountUpdater> updater = new();
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(x => x.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SecurityAuditStoreException("offline", new InvalidOperationException()));
        ChangePasswordHandler handler = new(updater.Object, writer.Object, new TestRequestContext());

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.ProviderFailure);
        updater.Verify(x => x.UserHasPasswordAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Duplicate_request_does_not_repeat_the_password_mutation()
    {
        Mock<IUserAccountUpdater> updater = new();
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(x => x.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.AlreadyRecorded);
        ChangePasswordHandler handler = new(updater.Object, writer.Object, new TestRequestContext());

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
        updater.Setup(x => x.ChangePasswordAsync("user-42", "old", "new-password", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        List<SecurityAuditEvent> events = [];
        RecordingWriter writer = new(events);
        ChangePasswordHandler handler = new(updater.Object, writer, new TestRequestContext());

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Success>().Which.Value.Should().BeTrue();
        events.Select(x => x.Outcome).Should().ContainInOrder(SecurityAuditOutcome.Started, SecurityAuditOutcome.Succeeded);
        updater.Verify(x => x.ChangePasswordAsync("user-42", "old", "new-password", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Failed_mutation_writes_a_redacted_failure_outcome()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        updater.Setup(x => x.ChangePasswordAsync("user-42", "old", "new-password", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        List<SecurityAuditEvent> events = [];
        RecordingWriter writer = new(events);
        ChangePasswordHandler handler = new(updater.Object, writer, new TestRequestContext());

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>().Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidCredentials);
        events[^1].Outcome.Should().Be(SecurityAuditOutcome.Failed);
        events[^1].RedactedReason.Should().Be("mutation_rejected");
        events[^1].Resource.Should().Be("user/user-42");
    }

    [Fact]
    public async Task Completion_store_failure_withholds_success_without_replaying_the_mutation()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        updater.Setup(x => x.ChangePasswordAsync("user-42", "old", "new-password", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<ISecurityAuditWriter> writer = new();
        writer.SetupSequence(x => x.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded)
            .ThrowsAsync(new SecurityAuditStoreException("offline", new InvalidOperationException()));
        ChangePasswordHandler handler = new(updater.Object, writer.Object, new TestRequestContext());

        AuthenticationResult<bool> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.ProviderFailure);
        updater.Verify(x => x.ChangePasswordAsync("user-42", "old", "new-password", It.IsAny<CancellationToken>()), Times.Once);
        writer.Verify(x => x.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static ChangePasswordCommand Command() => new("user-42", "old", "new-password", "operation-42");

    private sealed class RecordingWriter(List<SecurityAuditEvent> events) : ISecurityAuditWriter
    {
        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            events.Add(auditEvent);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }
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
