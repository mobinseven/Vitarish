using System.Threading.Channels;
using Moq;
using Vitarish.Audit.Domain;
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
            Checksum = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData("content"u8.ToArray())),
            ContentType = "application/pdf"
        };

        Func<Task> save = () => service.SaveFile(channel.Reader, file);

        await save.Should().ThrowAsync<IOException>();
        audit.Verify(value => value.RecordImportAsync(
            null,
            SecurityAuditOutcome.Failed,
            SecurityAuditReasonCatalog.EndpointException,
            CancellationToken.None), Times.Once);
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
