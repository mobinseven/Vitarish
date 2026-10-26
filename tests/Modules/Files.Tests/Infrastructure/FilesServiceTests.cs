using System.Security.Cryptography;
using System.Threading.Channels;
using Moq;
using Vitarish.Audit.Domain;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Infrastructure.Services;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Files.Tests.Infrastructure;

public sealed class FilesServiceTests
{
    [Fact]
    public async Task SaveFile_records_a_failed_import_when_the_bounded_input_stream_fails()
    {
        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        channel.Writer.TryComplete(new IOException("input failed"));
        Mock<IFileTransferAudit> audit = new();
        FilesService service = new(
            Mock.Of<IFileInformationService>(),
            Mock.Of<IObjectStorageService>(),
            Mock.Of<IFileIntegrityService>(),
            audit.Object,
            new TestRequestContext());
        FileInformationReadDto file = new()
        {
            Checksum = Convert.ToHexString(SHA256.HashData("content"u8.ToArray())),
            ContentType = "application/pdf",
            FileName = "referral.pdf"
        };

        Func<Task> save = () => service.SaveFile(channel.Reader, file);

        await save.Should().ThrowAsync<IOException>();
        audit.Verify(value => value.RecordImportAsync(
            null,
            "referral.pdf",
            0L,
            SecurityAuditOutcome.Failed,
            SecurityAuditReasonCatalog.EndpointException,
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task SaveFile_stores_the_normalized_file_name()
    {
        byte[] content = "content"u8.ToArray();
        string checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        Guid savedId = Guid.NewGuid();
        Mock<IFileInformationService> fileInformation = new();
        fileInformation.Setup(value => value.GetListAsync(It.IsAny<FileInformationDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<FileInformationReadDto>>.Success([]));
        fileInformation.Setup(value => value.AddAsync(It.IsAny<FileInformationWriteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<FileInformationReadDto>.Success(new FileInformationReadDto
            {
                Id = savedId,
                Owner = "tenant-a",
                ContentType = "application/pdf",
                Checksum = checksum,
                FileName = "referral.pdf",
                Metadata = new FileMetadata { Size = content.Length }
            }));
        Mock<IFileIntegrityService> integrity = new();
        integrity.Setup(value => value.BeginEnrollmentAsync(It.IsAny<FileInformationReadDto>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileIntegrityVerificationResult(FileIntegrityState.Pending, "files/key", content.Length, "application/pdf"));
        integrity.Setup(value => value.VerifyAsync(It.IsAny<FileInformationReadDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileIntegrityVerificationResult(FileIntegrityState.Verified, "files/key", content.Length, "application/pdf"));
        Mock<IObjectStorageService> storage = new();
        storage.Setup(value => value.ExistsAsync("files/key", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<IFileTransferAudit> audit = new();
        FilesService service = new(fileInformation.Object, storage.Object, integrity.Object, audit.Object, new TestRequestContext());
        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        _ = channel.Writer.TryWrite(content);
        channel.Writer.TryComplete();
        FileInformationReadDto file = new()
        {
            Checksum = checksum,
            ContentType = "application/pdf",
            FileName = "C:\\scans\\referral\u202E.pdf",
            Metadata = new FileMetadata { Size = content.Length }
        };

        FileInformationReadDto? saved = await service.SaveFile(channel.Reader, file);

        saved.Should().NotBeNull();
        fileInformation.Verify(value => value.AddAsync(
            It.Is<FileInformationWriteDto>(written => written.FileName == "referral.pdf" && written.Owner == "tenant-a" && written.Checksum == checksum),
            It.IsAny<CancellationToken>()), Times.Once);
        audit.Verify(value => value.RecordImportAsync(
            savedId,
            "C:\\scans\\referral\u202E.pdf",
            content.Length,
            SecurityAuditOutcome.Succeeded,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveFile_records_the_uploading_callers_name_on_a_dedup_hit_without_rewriting_the_shared_row()
    {
        byte[] content = "content"u8.ToArray();
        string checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        FileInformationReadDto existing = new()
        {
            Id = Guid.NewGuid(),
            Owner = "tenant-a",
            Checksum = checksum,
            ContentType = "application/pdf",
            FileName = "first-uploader.pdf",
            Metadata = new FileMetadata { Size = content.Length }
        };
        Mock<IFileInformationService> fileInformation = new();
        fileInformation.Setup(value => value.GetListAsync(It.IsAny<FileInformationDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<FileInformationReadDto>>.Success([existing]));
        Mock<IFileIntegrityService> integrity = new();
        integrity.Setup(value => value.VerifyAsync(existing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileIntegrityVerificationResult(FileIntegrityState.Verified, "files/key", content.Length, "application/pdf"));
        Mock<IFileTransferAudit> audit = new();
        FilesService service = new(
            fileInformation.Object,
            Mock.Of<IObjectStorageService>(),
            integrity.Object,
            audit.Object,
            new TestRequestContext());
        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        _ = channel.Writer.TryWrite(content);
        channel.Writer.TryComplete();
        FileInformationReadDto file = new()
        {
            Checksum = checksum,
            ContentType = "application/pdf",
            FileName = "second-uploader.pdf",
            Metadata = new FileMetadata { Size = content.Length }
        };

        FileInformationReadDto? saved = await service.SaveFile(channel.Reader, file);

        saved.Should().BeSameAs(existing);
        audit.Verify(value => value.RecordImportAsync(
            existing.Id,
            "second-uploader.pdf",
            content.Length,
            SecurityAuditOutcome.Succeeded,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        fileInformation.Verify(value => value.AddAsync(It.IsAny<FileInformationWriteDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class TestRequestContext : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => "tenant-a";
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => null;
        public string? AdditionalInfo => null;
        public System.Security.Claims.ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { TenantId = TenantId };
    }
}
