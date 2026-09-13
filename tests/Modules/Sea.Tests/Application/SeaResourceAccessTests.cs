using Moq;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Application.Access;
using Vitarish.Sea.Application.Agents;

namespace Vitarish.Sea.Tests.Application;

public sealed class SeaResourceAccessTests
{
    [Fact]
    public async Task Missing_trusted_context_denies_without_evaluator_call()
    {
        var evaluator = new Mock<ISeaManagementAccessEvaluator>(MockBehavior.Strict);
        var context = RequestContext(tenantId: null, userId: null);
        var access = new SeaResourceAccess(evaluator.Object, context.Object);

        bool allowed = await access.CanReadAttachmentAsync(Guid.NewGuid());

        allowed.Should().BeFalse();
        evaluator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Attachment_authorization_uses_trusted_context_and_resource()
    {
        Guid attachmentId = Guid.NewGuid();
        var evaluator = new Mock<ISeaManagementAccessEvaluator>(MockBehavior.Strict);
        evaluator
            .Setup(value => value.CanWriteAttachmentAsync(
                It.Is<SeaManagementAgentSubject>(subject => subject.TenantId == "tenant-a" && subject.UserId == "user-a"),
                attachmentId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var access = new SeaResourceAccess(evaluator.Object, RequestContext("tenant-a", "user-a").Object);

        bool allowed = await access.CanWriteAttachmentAsync(attachmentId);

        allowed.Should().BeTrue();
        evaluator.Verify(value => value.CanWriteAttachmentAsync(
            It.Is<SeaManagementAgentSubject>(subject => subject.TenantId == "tenant-a" && subject.UserId == "user-a"),
            attachmentId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Mock<IRequestContext> RequestContext(string? tenantId, string? userId)
    {
        var context = new Mock<IRequestContext>();
        context.SetupGet(value => value.TenantId).Returns(tenantId);
        context.SetupGet(value => value.UserId).Returns(userId);
        return context;
    }
}
