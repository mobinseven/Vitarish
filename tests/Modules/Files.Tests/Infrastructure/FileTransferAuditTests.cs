using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Files.Infrastructure.Services;
using Vitarish.Requests.Application.Abstractions;

namespace Files.Tests.Infrastructure;

public sealed class FileTransferAuditTests
{
    private static readonly Guid FileId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public async Task Upload_and_download_records_carry_the_normalized_file_name()
    {
        List<SecurityAuditEvent> written = [];
        FileTransferAudit audit = new(Writer(written), RequestContext());

        await audit.RecordImportAsync(FileId, "C:\\scans\\referral\u202E.pdf", SecurityAuditOutcome.Succeeded, null);
        await audit.RecordExportAsync(FileId, "referral.pdf", SecurityAuditOutcome.Succeeded, null);

        written.Select(item => (item.EventName, item.ResourceName)).Should().Equal(
            (SecurityAuditEventCatalog.ImportEvent, "referral.pdf"),
            (SecurityAuditEventCatalog.ExportEvent, "referral.pdf"));
    }

    [Fact]
    public async Task A_transfer_without_a_usable_name_records_no_name()
    {
        List<SecurityAuditEvent> written = [];
        FileTransferAudit audit = new(Writer(written), RequestContext());

        await audit.RecordImportAsync(null, null, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.ContentTooLarge);
        await audit.RecordExportAsync(FileId, "   ", SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.Unauthorized);
        await audit.RecordIntegrityVerificationAsync(FileId, SecurityAuditOutcome.Succeeded, null);

        written.Should().HaveCount(3).And.OnlyContain(item => item.ResourceName == null);
    }

    private static ISecurityAuditWriter Writer(List<SecurityAuditEvent> written)
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAuditEvent, CancellationToken>((item, _) => written.Add(item))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        return writer.Object;
    }

    private static IRequestContext RequestContext()
    {
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(value => value.TenantId).Returns("tenant-a");
        requestContext.SetupGet(value => value.UserId).Returns("user-1");
        requestContext.SetupGet(value => value.RequestId).Returns("request-1");
        return requestContext.Object;
    }
}
