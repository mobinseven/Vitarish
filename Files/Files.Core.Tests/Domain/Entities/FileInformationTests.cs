namespace Vitarish.Files.Core.Tests.Domain.Entities;

using System.Text.Json;
using Vitarish.Files.Core.Domain.Entities;
using Vitarish.Files.Core.Domain.Models;

public class FileInformationTests
{
    [Fact]
    public void FileInformation_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var fileInfo = new FileInformation { ContentType = "image/png" };

        // Assert
        Assert.Equal("*", fileInfo.AllowedOrigins);
        Assert.Equal("image/png", fileInfo.ContentType);
        Assert.Null(fileInfo.Thumbnail);
        Assert.Equal(string.Empty, fileInfo.Extension);
        Assert.Equal(0, fileInfo.Size);
        Assert.Null(fileInfo.Checksum);
        Assert.Null(fileInfo.Url);
        Assert.Null(fileInfo.Metadata);
        Assert.Null(fileInfo.TenantId);
        Assert.Null(fileInfo.Owner);
        Assert.Null(fileInfo.Tag);
    }

    [Fact]
    public void FileInformation_SetMetadata_ParsesExtensionAndSize()
    {
        // Arrange
        var fileInfo = new FileInformation { ContentType = "application/pdf" };
        var metadata = new FileMetadata { Extension = ".pdf", Size = 1024 };
        var metadataJson = JsonSerializer.Serialize(metadata);

        // Act
        fileInfo.Metadata = metadataJson;

        // Assert
        Assert.Equal(".pdf", fileInfo.Extension);
        Assert.Equal(1024, fileInfo.Size);
    }

    [Fact]
    public void FileInformation_SetNullMetadata_ResetsExtensionAndSize()
    {
        // Arrange
        var fileInfo = new FileInformation { ContentType = "text/plain" };
        var metadata = new FileMetadata { Extension = ".txt", Size = 100 };
        fileInfo.Metadata = JsonSerializer.Serialize(metadata);

        // Act
        fileInfo.Metadata = null;

        // Assert
        Assert.Equal(string.Empty, fileInfo.Extension);
        Assert.Equal(0, fileInfo.Size);
    }

    [Fact]
    public void FileInformation_SetEmptyMetadata_ResetsExtensionAndSize()
    {
        // Arrange
        var fileInfo = new FileInformation { ContentType = "text/plain" };

        // Act
        fileInfo.Metadata = "";

        // Assert
        Assert.Equal(string.Empty, fileInfo.Extension);
        Assert.Equal(0, fileInfo.Size);
    }

    [Fact]
    public void FileInformation_SetInvalidJsonMetadata_ResetsExtensionAndSize()
    {
        // Arrange
        var fileInfo = new FileInformation { ContentType = "text/plain" };

        // Act
        fileInfo.Metadata = "invalid json";

        // Assert
        Assert.Equal(string.Empty, fileInfo.Extension);
        Assert.Equal(0, fileInfo.Size);
    }

    [Fact]
    public void FileInformation_Owner_MapsToTenantId()
    {
        // Arrange
        var fileInfo = new FileInformation { ContentType = "image/jpeg" };

        // Act
        fileInfo.Owner = "tenant-123";

        // Assert
        Assert.Equal("tenant-123", fileInfo.TenantId);
        Assert.Equal("tenant-123", fileInfo.Owner);
    }

    [Fact]
    public void FileInformation_TenantId_MapsToOwner()
    {
        // Arrange
        var fileInfo = new FileInformation { ContentType = "image/jpeg" };

        // Act
        fileInfo.TenantId = "tenant-456";

        // Assert
        Assert.Equal("tenant-456", fileInfo.TenantId);
        Assert.Equal("tenant-456", fileInfo.Owner);
    }

    [Fact]
    public void FileInformation_SetAllProperties_ValuesAreCorrect()
    {
        // Arrange & Act
        var fileInfo = new FileInformation
        {
            ContentType = "video/mp4",
            AllowedOrigins = "https://example.com",
            Thumbnail = "https://example.com/thumb.jpg",
            Checksum = "abc123",
            Url = "https://example.com/file.mp4",
            TenantId = "owner-1",
            Tag = "video"
        };

        // Assert
        Assert.Equal("video/mp4", fileInfo.ContentType);
        Assert.Equal("https://example.com", fileInfo.AllowedOrigins);
        Assert.Equal("https://example.com/thumb.jpg", fileInfo.Thumbnail);
        Assert.Equal("abc123", fileInfo.Checksum);
        Assert.Equal("https://example.com/file.mp4", fileInfo.Url);
        Assert.Equal("owner-1", fileInfo.Owner);
        Assert.Equal("video", fileInfo.Tag);
    }
}
