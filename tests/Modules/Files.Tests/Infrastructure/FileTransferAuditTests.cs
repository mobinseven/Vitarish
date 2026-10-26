using System.Security.Cryptography;
using System.Text;
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
    public async Task Upload_and_download_records_carry_the_normalized_name_hash_extension_and_size()
    {
        List<SecurityAuditEvent> written = [];
        FileTransferAudit audit = new(Writer(written), RequestContext());

        await audit.RecordImportAsync(FileId, "C:\\scans\\referral\u202E.pdf", 2048, SecurityAuditOutcome.Succeeded, null);
        await audit.RecordExportAsync(FileId, "referral.pdf", 2048, SecurityAuditOutcome.Succeeded, null);

        string expectedHash = ExpectedHash("referral.pdf");
        written.Select(item => (item.EventName, item.ResourceNameHash, item.ResourceExtension, item.ResourceSize)).Should().Equal(
            (SecurityAuditEventCatalog.ImportEvent, expectedHash, "pdf", 2048L),
            (SecurityAuditEventCatalog.ExportEvent, expectedHash, "pdf", 2048L));
    }

    [Fact]
    public async Task A_transfer_without_a_usable_name_records_no_name_hash_or_extension()
    {
        List<SecurityAuditEvent> written = [];
        FileTransferAudit audit = new(Writer(written), RequestContext());

        await audit.RecordImportAsync(null, null, null, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.ContentTooLarge);
        await audit.RecordExportAsync(FileId, "   ", 0, SecurityAuditOutcome.Denied, SecurityAuditReasonCatalog.Unauthorized);
        await audit.RecordIntegrityVerificationAsync(FileId, SecurityAuditOutcome.Succeeded, null);

        written.Should().HaveCount(3).And.OnlyContain(item => item.ResourceNameHash == null && item.ResourceExtension == null);
        written[0].ResourceSize.Should().BeNull();
        written[1].ResourceSize.Should().Be(0);
        written[2].ResourceSize.Should().BeNull();
    }

    [Fact]
    public async Task A_rejected_audit_metadata_does_not_replace_the_transfer_result()
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("rejected"));
        FileTransferAudit audit = new(writer.Object, RequestContext());

        Func<Task> record = () => audit.RecordImportAsync(FileId, "referral.pdf", 2048, SecurityAuditOutcome.Succeeded, null);

        await record.Should().NotThrowAsync();
    }

    private static string ExpectedHash(string fileName) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fileName))).ToLowerInvariant();

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
