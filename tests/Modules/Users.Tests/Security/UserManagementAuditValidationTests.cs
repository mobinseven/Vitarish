using FluentAssertions;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;

namespace Vitarish.Users.Tests.Security;

public sealed class UserManagementAuditValidationTests
{
    [Fact]
    public async Task Invalid_email_creation_is_rejected_and_audited_without_contact_data()
    {
        (UsersManagementAudit audit, Mock<ISecurityAuditWriter> writer) = CreateAudit();
        CreateUserWithEmailCommandValidator validator = new(audit);

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            new CreateUserWithEmailCommand("not-an-email", null, null));

        result.IsValid.Should().BeFalse();
        SecurityAuditEvent auditEvent = VerifySingleEvent(writer, SecurityAuditEventCatalog.UserCreationFailureEvent);
        auditEvent.SubjectId.Should().BeNull();
        auditEvent.Resource.Should().Be(UsersSecurityAuditEvents.UnknownUserResource);
        auditEvent.RedactedReason.Should().Be(SecurityAuditReasonCatalog.MutationRejected);
        auditEvent.IdempotencyKey.Should().Be("request-id" + UsersSecurityAuditEvents.UserCreationFailureIdempotencySuffix);
    }

    [Fact]
    public async Task Iranian_landline_creation_is_rejected_and_audited()
    {
        (UsersManagementAudit audit, Mock<ISecurityAuditWriter> writer) = CreateAudit();
        CreateUserWithPhoneCommandValidator validator = new(audit);

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            new CreateUserWithPhoneCommand("+982112345678", null, null));

        result.IsValid.Should().BeFalse();
        SecurityAuditEvent auditEvent = VerifySingleEvent(writer, SecurityAuditEventCatalog.UserCreationFailureEvent);
        auditEvent.SubjectId.Should().BeNull();
        auditEvent.RedactedReason.Should().Be(SecurityAuditReasonCatalog.MutationRejected);
    }

    [Fact]
    public async Task Invalid_phone_edit_logs_both_edit_failure_and_denied_security_attribute_change()
    {
        (UsersManagementAudit audit, Mock<ISecurityAuditWriter> writer) = CreateAudit();
        SetUserPhoneNumberCommandValidator validator = new(audit);

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            new SetUserPhoneNumberCommand("user-42", "4155552671"));

        result.IsValid.Should().BeFalse();
        writer.Verify(value => value.WriteAsync(
            It.Is<SecurityAuditEvent>(item =>
                item.EventName == SecurityAuditEventCatalog.UserEditFailureEvent &&
                item.SubjectId == "user-42" &&
                item.Outcome == SecurityAuditOutcome.Denied &&
                item.RedactedReason == SecurityAuditReasonCatalog.MutationRejected),
            It.IsAny<CancellationToken>()), Times.Once);
        writer.Verify(value => value.WriteAsync(
            It.Is<SecurityAuditEvent>(item =>
                item.EventName == SecurityAuditEventCatalog.UserSecurityAttributeChangeEvent &&
                item.SubjectId == "user-42" &&
                item.Outcome == SecurityAuditOutcome.Denied &&
                item.RedactedReason == SecurityAuditReasonCatalog.MutationRejected),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Valid_creation_input_does_not_write_a_failure_event()
    {
        (UsersManagementAudit audit, Mock<ISecurityAuditWriter> writer) = CreateAudit();
        CreateUserWithPhoneCommandValidator validator = new(audit);

        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            new CreateUserWithPhoneCommand("+14155552671", "Given", "Family"));

        result.IsValid.Should().BeTrue();
        writer.Verify(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static SecurityAuditEvent VerifySingleEvent(Mock<ISecurityAuditWriter> writer, string eventName)
    {
        writer.Verify(value => value.WriteAsync(
            It.Is<SecurityAuditEvent>(item => item.EventName == eventName),
            It.IsAny<CancellationToken>()), Times.Once);
        return writer.Invocations
            .Select(invocation => (SecurityAuditEvent)invocation.Arguments[0])
            .Single(item => item.EventName == eventName);
    }

    private static (UsersManagementAudit Audit, Mock<ISecurityAuditWriter> Writer) CreateAudit()
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);

        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(value => value.RequestId).Returns("request-id");
        requestContext.SetupGet(value => value.TraceId).Returns("trace-id");
        requestContext.SetupGet(value => value.UserId).Returns("administrator");
        requestContext.SetupGet(value => value.TenantId).Returns("tenant-a");

        return (new UsersManagementAudit(writer.Object, requestContext.Object, TimeProvider.System), writer);
    }
}
