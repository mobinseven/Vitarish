using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Audit.Server;
using Vitarish.Files.Domain.Entities;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Infrastructure.Data;
using Vitarish.Files.Infrastructure.Options;
using Vitarish.Files.Infrastructure.Services;
using Vitarish.Requests.Application.Abstractions;

namespace Main.Tests;

public sealed class FileTransferAuditRetentionTests
{
    // EF InMemory verifies independence from source metadata deletion within one process; it does not prove database durability.
    [Fact]
    public async Task File_transfer_audit_name_remains_after_source_file_metadata_is_deleted()
    {
        const string tenantId = "file-audit-retention-tenant";
        const string filename = "گزارش\u200Cپزشکی.png";
        const string normalizedFilename = "گزارشپزشکی.png";
        const string fileNameDigestKey = "test-file-transfer-audit-retention-key-20261031";
        Guid fileId = Guid.NewGuid();
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(item => item.TenantId).Returns(tenantId);
        requestContext.SetupGet(item => item.UserId).Returns("file-audit-retention-user");
        requestContext.SetupGet(item => item.RequestId).Returns("file-audit-retention-request");

        DbContextOptions<FilesDbContext> filesOptions = new DbContextOptionsBuilder<FilesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using FilesDbContext files = new(
            filesOptions,
            [new FileIntegrityLifecycleInterceptor()],
            requestContext.Object,
            new HttpContextAccessor());
        FileInformation file = new()
        {
            Id = fileId,
            TenantId = tenantId,
            ContentType = "image/png",
            FileName = filename,
            Checksum = new string('a', 64)
        };
        files.Set<FileInformation>().Add(file);
        await files.SaveChangesAsync();

        ServiceCollection auditServices = new();
        _ = auditServices.AddLogging();
        _ = auditServices.AddSingleton(requestContext.Object);
        string auditDatabaseName = Guid.NewGuid().ToString("N");
        _ = auditServices.AddAuditInfrastructure(options => options.UseInMemoryDatabase(auditDatabaseName));
        using ServiceProvider auditProvider = auditServices.BuildServiceProvider();
        ISecurityAuditWriter writer = auditProvider.GetRequiredService<ISecurityAuditWriter>();
        FileTransferAudit audit = new(
            writer,
            requestContext.Object,
            new FileTransferAuditNameDigest(Options.Create(new FileTransferAuditKeyOptions { CurrentKey = fileNameDigestKey })));

        await audit.RecordImportAsync(fileId, file.FileName, 2048, SecurityAuditOutcome.Succeeded, null);

        await using (AuditDbContext beforeDeletion = auditProvider.GetRequiredService<AuditDbContext>())
        {
            SecurityAuditEvent record = await beforeDeletion.SecurityAuditEvents.SingleAsync(item =>
                item.Resource == FileTransferAuditContract.ResourcePrefix + fileId.ToString("N"));
            record.ResourceName.Should().Be(normalizedFilename);
        }

        files.Remove(file);
        await files.SaveChangesAsync();
        (await files.Set<FileInformation>().SingleOrDefaultAsync(item => item.Id == fileId)).Should().BeNull();

        await using AuditDbContext afterDeletion = auditProvider.GetRequiredService<AuditDbContext>();
        SecurityAuditEvent retained = await afterDeletion.SecurityAuditEvents.SingleAsync(item =>
            item.Resource == FileTransferAuditContract.ResourcePrefix + fileId.ToString("N"));
        retained.ResourceName.Should().Be(normalizedFilename);
        retained.ResourceNameHash.Should().NotBeNullOrWhiteSpace();
    }
}
