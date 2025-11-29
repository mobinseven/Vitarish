namespace Vitarish.Files.Core.Tests.Domain.Models;

using System.Text.Json;
using Vitarish.Files.Core.Domain.Models;

public class FileInformationDtoTests
{
    [Fact]
    public void FileInformationDto_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var dto = new FileInformationDto();

        // Assert
        Assert.Equal("*", dto.AllowedOrigins);
        Assert.Equal(string.Empty, dto.ContentType);
        Assert.Null(dto.Thumbnail);
        Assert.Null(dto.Checksum);
        Assert.Null(dto.Url);
        Assert.Null(dto.Owner);
        Assert.Null(dto.Tag);
        Assert.NotNull(dto.Metadata);
    }

    [Fact]
    public void FileInformationDto_Metadata_ReturnsNewMetadataIfNotSet()
    {
        // Arrange
        var dto = new FileInformationDto();

        // Act
        var metadata = dto.Metadata;

        // Assert
        Assert.NotNull(metadata);
        Assert.Null(metadata.Extension);
        Assert.Null(metadata.Size);
    }

    [Fact]
    public void FileInformationDto_SetMetadataJson_ParsesMetadata()
    {
        // Arrange
        var dto = new FileInformationDto();
        var metadataJson = JsonSerializer.Serialize(new FileMetadata { Extension = ".doc", Size = 5000 });

        // Act
        dto.MetadataJson = metadataJson;

        // Assert
        Assert.Equal(".doc", dto.Metadata.Extension);
        Assert.Equal(5000, dto.Metadata.Size);
    }

    [Fact]
    public void FileInformationDto_GetMetadataJson_SerializesMetadata()
    {
        // Arrange
        var dto = new FileInformationDto
        {
            Metadata = new FileMetadata { Extension = ".xlsx", Size = 3000 }
        };

        // Act
        var json = dto.MetadataJson;

        // Assert
        Assert.NotNull(json);
        Assert.Contains(".xlsx", json);
        Assert.Contains("3000", json);
    }

    [Fact]
    public void FileInformationDto_SetMetadata_ClearsMetadataJson()
    {
        // Arrange
        var dto = new FileInformationDto();
        dto.MetadataJson = JsonSerializer.Serialize(new FileMetadata { Extension = ".old" });

        // Act
        dto.Metadata = new FileMetadata { Extension = ".new" };
        var json = dto.MetadataJson;

        // Assert
        Assert.Contains(".new", json);
        Assert.DoesNotContain(".old", json);
    }

    [Fact]
    public void FileInformationDto_SetNullMetadata_SetsEmptyMetadata()
    {
        // Arrange
        var dto = new FileInformationDto();

        // Act
        dto.Metadata = null!;

        // Assert
        Assert.NotNull(dto.Metadata);
    }

    [Fact]
    public void FileInformationDto_SetInvalidMetadataJson_ReturnsEmptyMetadata()
    {
        // Arrange
        var dto = new FileInformationDto();

        // Act
        dto.MetadataJson = "not valid json";
        var metadata = dto.Metadata;

        // Assert
        Assert.NotNull(metadata);
    }

    [Fact]
    public void FileInformationDto_SetProperties_ValuesAreCorrect()
    {
        // Arrange
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var dto = new FileInformationDto
        {
            Id = id,
            CreatedAt = createdAt,
            AllowedOrigins = "https://example.com",
            ContentType = "application/json",
            Thumbnail = "https://example.com/thumb.png",
            Checksum = "checksum123",
            Url = "https://example.com/data.json",
            Owner = "user-456",
            Tag = "data"
        };

        // Assert
        Assert.Equal(id, dto.Id);
        Assert.Equal(createdAt, dto.CreatedAt);
        Assert.Equal("https://example.com", dto.AllowedOrigins);
        Assert.Equal("application/json", dto.ContentType);
        Assert.Equal("https://example.com/thumb.png", dto.Thumbnail);
        Assert.Equal("checksum123", dto.Checksum);
        Assert.Equal("https://example.com/data.json", dto.Url);
        Assert.Equal("user-456", dto.Owner);
        Assert.Equal("data", dto.Tag);
    }
}
