using FluentAssertions;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Tests.Security;

public sealed class AuthenticationAttemptAuditorTests
{
    [Theory]
    [InlineData(AuthenticationMethod.Email, false)]
    [InlineData(AuthenticationMethod.Email, true)]
    [InlineData(AuthenticationMethod.Phone, false)]
    [InlineData(AuthenticationMethod.Phone, true)]
    [InlineData(AuthenticationMethod.Passkey, false)]
    [InlineData(AuthenticationMethod.Passkey, true)]
    public async Task Records_success_and_failure_without_submitted_authentication_material(
        AuthenticationMethod method,
        bool succeeded)
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(value => value.UserId).Returns((string?)null);
        requestContext.SetupGet(value => value.RequestId).Returns("request-id");
        requestContext.SetupGet(value => value.TraceId).Returns("trace-id");
        requestContext.SetupGet(value => value.TenantId).Returns("tenant-a");
        Mock<IAuthenticationUtcClock> clock = new();
        clock.SetupGet(value => value.UtcNow).Returns(new DateTime(2026, 9, 28, 7, 0, 0, DateTimeKind.Utc));
        AuthenticationAttemptAuditor auditor = new(writer.Object, requestContext.Object, clock.Object);

        await auditor.RecordAsync(method, "user-42", succeeded, CancellationToken.None);

        SecurityAuditEvent auditEvent = writer.Invocations
            .Select(invocation => (SecurityAuditEvent)invocation.Arguments[0])
            .Single();
        auditEvent.EventName.Should().Be(method switch
        {
            AuthenticationMethod.Email => AuthenticationSecurityAuditEvents.EmailLogin,
            AuthenticationMethod.Phone => AuthenticationSecurityAuditEvents.PhoneLogin,
            AuthenticationMethod.Passkey => AuthenticationSecurityAuditEvents.PasskeyLogin,
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        });
        auditEvent.EventCategory.Should().Be(SecurityAuditEventCatalog.AuthenticationCategory);
        auditEvent.Outcome.Should().Be(succeeded ? SecurityAuditOutcome.Succeeded : SecurityAuditOutcome.Denied);
        auditEvent.ActorId.Should().BeNull();
        auditEvent.SubjectId.Should().Be("user-42");
        auditEvent.TenantId.Should().Be("tenant-a");
        auditEvent.Operation.Should().Be(method switch
        {
            AuthenticationMethod.Email => AuthenticationSecurityAuditEvents.EmailLoginOperation,
            AuthenticationMethod.Phone => AuthenticationSecurityAuditEvents.PhoneLoginOperation,
            AuthenticationMethod.Passkey => AuthenticationSecurityAuditEvents.PasskeyLoginOperation,
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        });
        auditEvent.OccurredAt.Kind.Should().Be(DateTimeKind.Utc);
        auditEvent.RedactedReason.Should().Be(succeeded ? null : SecurityAuditReasonCatalog.AuthenticationFailed);
        auditEvent.IdempotencyKey.Should().Be("request-id" + AuthenticationSecurityAuditEvents.AuthenticationAttemptIdempotencySuffix + auditEvent.Operation);
        auditEvent.Resource.Should().Be(AuthenticationSecurityAuditEvents.Resource);
    }

    [Fact]
    public async Task Unknown_login_identifier_uses_anonymous_subject_without_copying_the_identifier()
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(value => value.RequestId).Returns("request-id");
        requestContext.SetupGet(value => value.TenantId).Returns("tenant-a");
        AuthenticationAttemptAuditor auditor = new(writer.Object, requestContext.Object, new TestAuthenticationUtcClock());

        await auditor.RecordAsync(AuthenticationMethod.Email, null, succeeded: false, CancellationToken.None);

        SecurityAuditEvent auditEvent = writer.Invocations
            .Select(invocation => (SecurityAuditEvent)invocation.Arguments[0])
            .Single();
        auditEvent.ActorKind.Should().BeNull();
        auditEvent.ActorId.Should().BeNull();
        auditEvent.SubjectId.Should().BeNull();
        auditEvent.Resource.Should().Be(AuthenticationSecurityAuditEvents.Resource);
        auditEvent.RedactedReason.Should().Be(SecurityAuditReasonCatalog.AuthenticationFailed);
    }

    private sealed class TestAuthenticationUtcClock : IAuthenticationUtcClock
    {
        public DateTime UtcNow => new(2026, 9, 28, 7, 0, 0, DateTimeKind.Utc);
    }
}
