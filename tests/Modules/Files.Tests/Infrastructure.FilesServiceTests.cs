using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Threading.Channels;
using Vitarish.Files.Infrastructure.Services;

namespace Vitarish.Files.Tests;

/// <summary>
/// Unit tests for the <see cref="FilesService"/> class.
/// </summary>
public sealed class FilesServiceTests
{
    [Fact]
    public async Task SaveFile_WithValidData_UploadsAndStoresMetadata()
    {
        // Arrange
        byte[] payload = System.Text.Encoding.UTF8.GetBytes("Hello, storage!");
        string checksum = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        IOptions<FilesOptions> options = CreateOptions();
        var fileInformationService = new InMemoryFileInformationService();
        var storage = new RecordingObjectStorageService();
        var tenantAccessor = new TestTenantAccessor("tenant-123");

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
            tenantAccessor);

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        await channel.Writer.WriteAsync(payload);
        channel.Writer.Complete();

        FileInformationReadDto request = new()
        {
            AllowedOrigins = "https://app.example",
            ContentType = "image/png",
            Checksum = checksum,
            Metadata = new FileMetadata()
        };

        // Act
        FileInformationReadDto? result = await service.SaveFile(channel.Reader, request);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Owner.Should().Be("tenant-123");
        _ = result.Url.Should().NotBeNullOrEmpty();
        _ = result.Metadata.Extension.Should().Be("png");
        _ = result.Metadata.Size.Should().Be(payload.Length);
    }

    [Fact]
    public async Task SaveFile_WithoutChecksum_ThrowsInvalidDataException()
    {
        // Arrange
        IOptions<FilesOptions> options = CreateOptions();
        var fileInformationService = new InMemoryFileInformationService();
        var storage = new RecordingObjectStorageService();
        var tenantAccessor = new TestTenantAccessor("tenant-123");

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
            tenantAccessor);

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        channel.Writer.Complete();

        FileInformationReadDto request = new()
        {
            AllowedOrigins = "https://app.example",
            ContentType = "image/png",
            Checksum = null, // No checksum
            Metadata = new FileMetadata()
        };

        // Act
        Func<Task> act = async () => await service.SaveFile(channel.Reader, request);

        // Assert
        _ = await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Checksum is required");
    }

    [Fact]
    public async Task SaveFile_WithEmptyChecksum_ThrowsInvalidDataException()
    {
        // Arrange
        IOptions<FilesOptions> options = CreateOptions();
        var fileInformationService = new InMemoryFileInformationService();
        var storage = new RecordingObjectStorageService();
        var tenantAccessor = new TestTenantAccessor("tenant-123");

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
            tenantAccessor);

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        channel.Writer.Complete();

        FileInformationReadDto request = new()
        {
            AllowedOrigins = "https://app.example",
            ContentType = "image/png",
            Checksum = "",
            Metadata = new FileMetadata()
        };

        // Act
        Func<Task> act = async () => await service.SaveFile(channel.Reader, request);

        // Assert
        _ = await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("Checksum is required");
    }

    [Fact]
    public async Task SaveFile_WithoutTenant_ThrowsInvalidOperationException()
    {
        // Arrange
        IOptions<FilesOptions> options = CreateOptions();
        var fileInformationService = new InMemoryFileInformationService();
        var storage = new RecordingObjectStorageService();
        var tenantAccessor = new TestTenantAccessor(null); // No tenant

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
            tenantAccessor);

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        channel.Writer.Complete();

        FileInformationReadDto request = new()
        {
            ContentType = "image/png",
            Checksum = "abc123",
            Metadata = new FileMetadata()
        };

        // Act
        Func<Task> act = async () => await service.SaveFile(channel.Reader, request);

        // Assert
        _ = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tenant*");
    }

    [Fact]
    public async Task SaveFile_WithMismatchedOwner_ThrowsInvalidOperationException()
    {
        // Arrange
        IOptions<FilesOptions> options = CreateOptions();
        var fileInformationService = new InMemoryFileInformationService();
        var storage = new RecordingObjectStorageService();
        var tenantAccessor = new TestTenantAccessor("tenant-123");

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
            tenantAccessor);

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        channel.Writer.Complete();

        FileInformationReadDto request = new()
        {
            ContentType = "image/png",
            Checksum = "abc123",
            Owner = "different-tenant", // Mismatched
            Metadata = new FileMetadata()
        };

        // Act
        Func<Task> act = async () => await service.SaveFile(channel.Reader, request);

        // Assert
        _ = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*owner*tenant*");
    }

    [Fact]
    public async Task SaveFile_WithDuplicateChecksum_ReturnsExistingFile()
    {
        // Arrange
        byte[] payload = System.Text.Encoding.UTF8.GetBytes("Duplicate content");
        string checksum = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        IOptions<FilesOptions> options = CreateOptions();
        var fileInformationService = new InMemoryFileInformationService();
        var storage = new RecordingObjectStorageService();
        var tenantAccessor = new TestTenantAccessor("tenant-123");

        // Seed existing file with same checksum
        var existingFile = new FileInformationReadDto
        {
            Id = Guid.NewGuid(),
            Checksum = checksum,
            Owner = "tenant-123",
            Url = "https://files.example/uploads/existing-file.png",
            ContentType = "image/png",
            Metadata = new FileMetadata { Size = 100, Extension = "png" }
        };
        fileInformationService.Seed("existing-file.png", existingFile);

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
            tenantAccessor);

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        await channel.Writer.WriteAsync(payload);
        channel.Writer.Complete();

        FileInformationReadDto request = new()
        {
            ContentType = "image/png",
            Checksum = checksum,
            Metadata = new FileMetadata()
        };

        // Act
        FileInformationReadDto? result = await service.SaveFile(channel.Reader, request);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().BeSameAs(existingFile);
        _ = storage.Uploads.Should().BeEmpty(); // No upload happened
    }

    [Fact]
    public async Task SaveFile_WithDuplicateChecksumForDifferentTenant_ClonesFile()
    {
        // Arrange
        byte[] payload = System.Text.Encoding.UTF8.GetBytes("Duplicate content");
        string checksum = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        IOptions<FilesOptions> options = CreateOptions();
        var fileInformationService = new InMemoryFileInformationService();
        var storage = new RecordingObjectStorageService();
        var tenantAccessor = new TestTenantAccessor("tenant-456"); // Different tenant

        // Seed existing file with same checksum but different tenant
        var existingFile = new FileInformationReadDto
        {
            Id = Guid.NewGuid(),
            Checksum = checksum,
            Owner = "tenant-123", // Different tenant
            Url = "https://files.example/uploads/existing-file.png",
            ContentType = "image/png",
            Metadata = new FileMetadata { Size = 100, Extension = "png" }
        };
        fileInformationService.Seed("existing-file.png", existingFile);

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
            tenantAccessor);

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        await channel.Writer.WriteAsync(payload);
        channel.Writer.Complete();

        FileInformationReadDto request = new()
        {
            ContentType = "image/png",
            Checksum = checksum,
            Metadata = new FileMetadata()
        };

        // Act
        FileInformationReadDto? result = await service.SaveFile(channel.Reader, request);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Owner.Should().Be("tenant-456"); // New tenant
        _ = result.Url.Should().Be(existingFile.Url); // Same URL (file reused)
        _ = storage.Uploads.Should().BeEmpty(); // No upload happened (file cloned)
    }

    [Fact]
    public async Task SaveFile_WithoutContentType_ThrowsInvalidDataException()
    {
        // Arrange
        byte[] payload = System.Text.Encoding.UTF8.GetBytes("Test content");
        string checksum = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        IOptions<FilesOptions> options = CreateOptions();
        var fileInformationService = new InMemoryFileInformationService();
        var storage = new RecordingObjectStorageService();
        var tenantAccessor = new TestTenantAccessor("tenant-123");

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
            tenantAccessor);

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        await channel.Writer.WriteAsync(payload);
        channel.Writer.Complete();

        FileInformationReadDto request = new()
        {
            Checksum = checksum,
            ContentType = "", // Empty content type
            Metadata = new FileMetadata()
        };

        // Act
        Func<Task> act = async () => await service.SaveFile(channel.Reader, request);

        // Assert
        _ = await act.Should().ThrowAsync<InvalidDataException>()
            .WithMessage("*Content type*");
    }

    [Fact]
    public async Task SaveFile_WithChecksumMismatch_ThrowsInvalidOperationException()
    {
        // Arrange
        byte[] payload = System.Text.Encoding.UTF8.GetBytes("Test content");
        string wrongChecksum = "wrong-checksum-value";

        IOptions<FilesOptions> options = CreateOptions();
        var fileInformationService = new InMemoryFileInformationService();
        var storage = new RecordingObjectStorageService();
        var tenantAccessor = new TestTenantAccessor("tenant-123");

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
            tenantAccessor);

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        await channel.Writer.WriteAsync(payload);
        channel.Writer.Complete();

        FileInformationReadDto request = new()
        {
            Checksum = wrongChecksum,
            ContentType = "text/plain",
            Metadata = new FileMetadata()
        };

        // Act
        Func<Task> act = async () => await service.SaveFile(channel.Reader, request);

        // Assert
        _ = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Checksum mismatch*");
    }

    [Fact]
    public async Task SaveFile_WithImageContentType_DoesNotAssignDefaultThumbnail()
    {
        // Arrange
        byte[] payload = System.Text.Encoding.UTF8.GetBytes("Image content");
        string checksum = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        IOptions<FilesOptions> options = CreateOptions();
        var fileInformationService = new InMemoryFileInformationService();
        var storage = new RecordingObjectStorageService();
        var tenantAccessor = new TestTenantAccessor("tenant-123");

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
            tenantAccessor);

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        await channel.Writer.WriteAsync(payload);
        channel.Writer.Complete();

        FileInformationReadDto request = new()
        {
            ContentType = "image/png",
            Checksum = checksum,
            Metadata = new FileMetadata()
        };

        // Act
        FileInformationReadDto? result = await service.SaveFile(channel.Reader, request);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Thumbnail.Should().BeNull();
    }

    [Fact]
    public async Task SaveFile_WithNonImageContentType_DoesNotAssignDefaultThumbnail()
    {
        // Arrange
        byte[] payload = System.Text.Encoding.UTF8.GetBytes("PDF content");
        string checksum = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        IOptions<FilesOptions> options = CreateOptions();
        var fileInformationService = new InMemoryFileInformationService();
        var storage = new RecordingObjectStorageService();
        var tenantAccessor = new TestTenantAccessor("tenant-123");

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
            tenantAccessor);

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        await channel.Writer.WriteAsync(payload);
        channel.Writer.Complete();

        FileInformationReadDto request = new()
        {
            ContentType = "application/pdf",
            Checksum = checksum,
            Metadata = new FileMetadata()
        };

        // Act
        FileInformationReadDto? result = await service.SaveFile(channel.Reader, request);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Thumbnail.Should().BeNull();
    }

    [Fact]
    public async Task SaveFile_WithCustomThumbnail_PreservesProvidedThumbnail()
    {
        // Arrange
        byte[] payload = System.Text.Encoding.UTF8.GetBytes("Image content");
        string checksum = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        IOptions<FilesOptions> options = CreateOptions();
        var fileInformationService = new InMemoryFileInformationService();
        var storage = new RecordingObjectStorageService();
        var tenantAccessor = new TestTenantAccessor("tenant-123");

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
            tenantAccessor);

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        await channel.Writer.WriteAsync(payload);
        channel.Writer.Complete();

        FileInformationReadDto request = new()
        {
            ContentType = "image/png",
            Checksum = checksum,
            Thumbnail = "https://example.com/custom-thumb.png", // Custom thumbnail
            Metadata = new FileMetadata()
        };

        // Act
        FileInformationReadDto? result = await service.SaveFile(channel.Reader, request);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result!.Thumbnail.Should().Be("https://example.com/custom-thumb.png");
    }

    private static IOptions<FilesOptions> CreateOptions() => Options.Create(new FilesOptions
    {
        Prefix = "files",
        BaseUrl = "https://files.example",
        UploadPath = "uploads"
    });
}
