using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Channels;
using Vorn.Files.Core.Domain.Models;
using Vorn.Files.Core.Presentation.Interfaces;
using Vorn.Files.Host.Domain.Options;
using Vorn.Files.Host.Infrastructure.Interfaces;
using Vorn.Files.Host.Infrastructure.Services;

namespace Vorn.Files.Host.Tests.Infrastructure.Services;

public class FilesServiceTests
{
    [Fact]
    public async Task SaveFile_ClonesExistingFileMetadataForNewOwner()
    {
        // Arrange
        const string tenantId = "tenant-a";
        const string otherTenant = "tenant-b";
        string uploadPath = $"test-{Guid.NewGuid():N}";
        string baseDirectory = Path.Combine(Environment.CurrentDirectory, "wwwroot", uploadPath);

        const string existingChecksum = "abc123";

        FileMetadata existingMetadata = new()
        {
            Extension = "png",
            Size = 2048
        };

        FileInformationDto existing = new()
        {
            Id = Guid.NewGuid(),
            Owner = otherTenant,
            Url = "https://files.example/uploads/file-existing.png",
            Checksum = existingChecksum,
            Thumbnail = "https://files.example/uploads/file-existing.png?thumb=1",
            AllowedOrigins = "*",
            Tag = "existing",
            ContentType = "image/png",
            Metadata = existingMetadata
        };

        VornOptions options = new()
        {
            Files = new FilesOptions
            {
                Prefix = "file",
                BaseUrl = "https://files.example",
                UploadPath = uploadPath
            }
        };

        Mock<IFileInformationService> fileInfoService = new(MockBehavior.Strict);
        fileInfoService
            .Setup(s => s.GetListAsync(It.IsAny<FileInformationDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FileInformationDto> { existing });

        FileInformationDto? added = null;
        fileInfoService
            .Setup(s => s.AddAsync(It.IsAny<FileInformationDto>(), It.IsAny<CancellationToken>()))
            .Callback<FileInformationDto, CancellationToken>((dto, _) => added = dto)
            .ReturnsAsync((FileInformationDto dto, CancellationToken _) => dto);

        Mock<IThumbnailService> thumbnailService = new(MockBehavior.Strict);
        thumbnailService.Setup(s => s.Enqueue(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        Mock<IWebpConversionService> webpConversionService = new(MockBehavior.Strict);
        webpConversionService.Setup(s => s.Enqueue(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        TestTenantAccessor tenantAccessor = new(tenantId);

        FilesService sut = new(
            options,
            fileInfoService.Object,
            Mock.Of<ILogger<FilesService>>(),
            thumbnailService.Object,
            webpConversionService.Object,
            tenantAccessor);

        FileInformationDto given = new()
        {
            Checksum = existingChecksum,
            Owner = tenantId,
            AllowedOrigins = "https://tenant.example",
            Tag = "new-tag",
            ContentType = existing.ContentType
        };

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        channel.Writer.TryComplete();

        // Act
        FileInformationDto? result = await sut.SaveFile(channel.Reader, given, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(existing.Id, result!.Id);
        Assert.Equal(tenantId, result.Owner);
        Assert.Equal(existing.Url, result.Url);
        Assert.Equal(existing.Checksum, result.Checksum);
        Assert.Equal(existing.Thumbnail, result.Thumbnail);
        Assert.Equal(existing.Metadata, result.Metadata);
        Assert.Equal(existingMetadata.Size, result.Metadata.Size);
        Assert.Equal(existingMetadata.Extension, result.Metadata.Extension);
        Assert.Equal(given.AllowedOrigins, result.AllowedOrigins);
        Assert.Equal(given.Tag, result.Tag);

        Assert.NotNull(added);
        Assert.Same(result, added);

        fileInfoService.Verify(s => s.AddAsync(It.IsAny<FileInformationDto>(), It.IsAny<CancellationToken>()), Times.Once);
        thumbnailService.Verify(s => s.Enqueue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        webpConversionService.Verify(s => s.Enqueue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        Assert.False(Directory.Exists(baseDirectory));
    }

    private sealed class TestTenantAccessor : IMultiTenantContextAccessor<TenantInfo>, IMultiTenantContextSetter
    {
        public TestTenantAccessor(string tenantId) => MultiTenantContext = new MultiTenantContext<TenantInfo>
        {
            TenantInfo = new TenantInfo
            {
                Id = tenantId,
                Identifier = tenantId
            }
        };

        public IMultiTenantContext<TenantInfo> MultiTenantContext { get; private set; }

        IMultiTenantContext<TenantInfo> IMultiTenantContextAccessor<TenantInfo>.MultiTenantContext => MultiTenantContext;

        IMultiTenantContext IMultiTenantContextAccessor.MultiTenantContext => MultiTenantContext;

        IMultiTenantContext IMultiTenantContextSetter.MultiTenantContext
        {
            set => MultiTenantContext = (IMultiTenantContext<TenantInfo>?)value ?? new MultiTenantContext<TenantInfo>();
        }
    }
}
