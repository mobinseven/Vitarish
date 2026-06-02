using System.Text.Json;

namespace Files.Tests;

/// <summary>
/// Unit tests for the <see cref="FileInformationReadDto"/> record.
/// </summary>
public sealed class FileInformationDtoTests
{
    [Fact]
    public void FileInformationDto_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        FileInformationReadDto dto = new();

        // Assert
        _ = dto.AllowedOrigins.Should().Be("*");
        _ = dto.ContentType.Should().BeEmpty();
        _ = dto.Thumbnail.Should().BeNull();
        _ = dto.Checksum.Should().BeNull();
        _ = dto.Url.Should().BeNull();
        _ = dto.Owner.Should().BeNull();
        _ = dto.Tag.Should().BeNull();
    }

    [Fact]
    public void FileInformationDto_PropertiesAreSettable()
    {
        // Arrange
        FileInformationReadDto dto = new()
        {
            // Act
            AllowedOrigins = "https://example.com",
            ContentType = "image/png",
            Thumbnail = "https://example.com/thumb.png",
            Checksum = "abc123",
            Url = "https://example.com/file.png",
            Owner = "tenant-1",
            Tag = "profile"
        };

        // Assert
        _ = dto.AllowedOrigins.Should().Be("https://example.com");
        _ = dto.ContentType.Should().Be("image/png");
        _ = dto.Thumbnail.Should().Be("https://example.com/thumb.png");
        _ = dto.Checksum.Should().Be("abc123");
        _ = dto.Url.Should().Be("https://example.com/file.png");
        _ = dto.Owner.Should().Be("tenant-1");
        _ = dto.Tag.Should().Be("profile");
    }

    [Fact]
    public void Metadata_WhenNotSet_ReturnsEmptyFileMetadata()
    {
        // Arrange
        FileInformationReadDto dto = new();

        // Act
        FileMetadata metadata = dto.Metadata;

        // Assert
        _ = metadata.Should().NotBeNull();
        _ = metadata.Size.Should().Be(0);
        _ = metadata.Extension.Should().BeNull();
        _ = metadata.Dimensions.Should().BeNull();
    }

    [Fact]
    public void Metadata_WhenSet_ReturnsCorrectValue()
    {
        // Arrange
        FileInformationReadDto dto = new();
        FileMetadata expectedMetadata = new()
        {
            Size = 1024,
            Extension = "png",
            Dimensions = new FileDimensions { Width = 100, Height = 100 }
        };

        // Act
        dto.Metadata = expectedMetadata;

        // Assert
        _ = dto.Metadata.Should().NotBeNull();
        _ = dto.Metadata.Size.Should().Be(1024);
        _ = dto.Metadata.Extension.Should().Be("png");
        _ = dto.Metadata.Dimensions!.Width.Should().Be(100);
        _ = dto.Metadata.Dimensions.Height.Should().Be(100);
    }

    [Fact]
    public void Metadata_WhenSetToNull_ReturnsEmptyMetadata()
    {
        // Arrange
        FileInformationReadDto dto = new()
        {
            Metadata = new FileMetadata { Size = 1024 }
        };

        // Act
        dto.Metadata = null!;

        // Assert
        _ = dto.Metadata.Should().NotBeNull();
        _ = dto.Metadata.Size.Should().Be(0);
    }

    [Fact]
    public void MetadataJson_WhenMetadataSet_ReturnsSerializedJson()
    {
        // Arrange
        FileInformationReadDto dto = new()
        {
            Metadata = new FileMetadata
            {
                Size = 2048,
                Extension = "jpg"
            }
        };

        // Act
        string? json = dto.MetadataJson;

        // Assert
        _ = json.Should().NotBeNullOrEmpty();
        _ = json.Should().Contain("2048");
        _ = json.Should().Contain("jpg");
    }

    [Fact]
    public void MetadataJson_WhenSet_UpdatesMetadataProperty()
    {
        // Arrange
        FileInformationReadDto dto = new();
        string metadataJson = """{"size":4096,"extension":"gif"}""";

        // Act
        dto.MetadataJson = metadataJson;
        FileMetadata metadata = dto.Metadata;

        // Assert
        _ = metadata.Size.Should().Be(4096);
        _ = metadata.Extension.Should().Be("gif");
    }

    [Fact]
    public void MetadataJson_WhenSetWithDimensions_UpdatesMetadataProperty()
    {
        // Arrange
        FileInformationReadDto dto = new();
        string metadataJson = """{"size":1024,"extension":"png","dimensions":{"width":800,"height":600}}""";

        // Act
        dto.MetadataJson = metadataJson;
        FileMetadata metadata = dto.Metadata;

        // Assert
        _ = metadata.Size.Should().Be(1024);
        _ = metadata.Extension.Should().Be("png");
        _ = metadata.Dimensions.Should().NotBeNull();
        _ = metadata.Dimensions!.Width.Should().Be(800);
        _ = metadata.Dimensions.Height.Should().Be(600);
    }

    [Fact]
    public void MetadataJson_WhenSetWithInvalidJson_ReturnsEmptyMetadata()
    {
        // Arrange
        FileInformationReadDto dto = new();
        string invalidJson = "not valid json";

        // Act
        dto.MetadataJson = invalidJson;
        FileMetadata metadata = dto.Metadata;

        // Assert
        _ = metadata.Should().NotBeNull();
        _ = metadata.Size.Should().Be(0);
        _ = metadata.Extension.Should().BeNull();
    }

    [Fact]
    public void MetadataJson_WhenSetWithEmptyString_ReturnsEmptyMetadata()
    {
        // Arrange
        FileInformationReadDto dto = new()
        {
            // Act
            MetadataJson = ""
        };
        FileMetadata metadata = dto.Metadata;

        // Assert
        _ = metadata.Should().NotBeNull();
        _ = metadata.Size.Should().Be(0);
    }

    [Fact]
    public void MetadataJson_WhenSetWithWhitespace_ReturnsEmptyMetadata()
    {
        // Arrange
        FileInformationReadDto dto = new()
        {
            // Act
            MetadataJson = "   "
        };
        FileMetadata metadata = dto.Metadata;

        // Assert
        _ = metadata.Should().NotBeNull();
        _ = metadata.Size.Should().Be(0);
    }

    [Fact]
    public void MetadataJson_Caching_ReturnsConsistentValue()
    {
        // Arrange
        FileInformationReadDto dto = new()
        {
            Metadata = new FileMetadata { Size = 1024, Extension = "png" }
        };

        // Act
        string? json1 = dto.MetadataJson;
        string? json2 = dto.MetadataJson;

        // Assert
        _ = json1.Should().Be(json2);
    }

    [Fact]
    public void MetadataJson_WhenMetadataUpdated_CacheInvalidated()
    {
        // Arrange
        FileInformationReadDto dto = new()
        {
            Metadata = new FileMetadata { Size = 1024 }
        };
        string? originalJson = dto.MetadataJson;

        // Act
        dto.Metadata = new FileMetadata { Size = 2048 };
        string? newJson = dto.MetadataJson;

        // Assert
        _ = originalJson.Should().NotBe(newJson);
        _ = newJson.Should().Contain("2048");
    }

    [Fact]
    public void Metadata_WhenMetadataJsonSet_PreviousMetadataCleared()
    {
        // Arrange
        FileInformationReadDto dto = new()
        {
            Metadata = new FileMetadata { Size = 1024, Extension = "png" },
            // Act
            MetadataJson = """{"size":512,"extension":"jpg"}"""
        };
        FileMetadata metadata = dto.Metadata;

        // Assert
        _ = metadata.Size.Should().Be(512);
        _ = metadata.Extension.Should().Be("jpg");
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://example.com")]
    [InlineData("https://app.example.com,https://api.example.com")]
    public void FileInformationDto_AcceptsVariousAllowedOrigins(string allowedOrigins)
    {
        // Arrange & Act
        FileInformationReadDto dto = new()
        {
            AllowedOrigins = allowedOrigins
        };

        // Assert
        _ = dto.AllowedOrigins.Should().Be(allowedOrigins);
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("application/pdf")]
    [InlineData("video/mp4")]
    [InlineData("application/octet-stream")]
    public void FileInformationDto_AcceptsVariousContentTypes(string contentType)
    {
        // Arrange & Act
        FileInformationReadDto dto = new()
        {
            ContentType = contentType
        };

        // Assert
        _ = dto.ContentType.Should().Be(contentType);
    }

    [Fact]
    public void FileInformationDto_JsonSerialization_IgnoresMetadataJson()
    {
        // Arrange
        FileInformationReadDto dto = new()
        {
            ContentType = "image/png",
            Metadata = new FileMetadata { Size = 1024, Extension = "png" }
        };

        // Act
        string json = JsonSerializer.Serialize(dto);

        // Assert
        _ = json.Should().NotContain("metadataJson");
        _ = json.Should().Contain("metadata");
    }

    [Fact]
    public void FileInformationDto_JsonDeserialization_WorksCorrectly()
    {
        // Arrange
        string json = """
        {
            "allowedOrigins": "https://example.com",
            "contentType": "image/png",
            "thumbnail": "https://example.com/thumb.png",
            "checksum": "abc123",
            "url": "https://example.com/file.png",
            "owner": "tenant-1",
            "metadata": {"size": 1024, "extension": "png"},
            "tag": "profile"
        }
        """;

        // Act
        FileInformationReadDto? dto = JsonSerializer.Deserialize<FileInformationReadDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Assert
        _ = dto.Should().NotBeNull();
        _ = dto!.AllowedOrigins.Should().Be("https://example.com");
        _ = dto.ContentType.Should().Be("image/png");
        _ = dto.Thumbnail.Should().Be("https://example.com/thumb.png");
        _ = dto.Checksum.Should().Be("abc123");
        _ = dto.Url.Should().Be("https://example.com/file.png");
        _ = dto.Owner.Should().Be("tenant-1");
        _ = dto.Metadata.Size.Should().Be(1024);
        _ = dto.Metadata.Extension.Should().Be("png");
        _ = dto.Tag.Should().Be("profile");
    }

    [Fact]
    public void FileInformationDto_PartialJsonDeserialization_WorksCorrectly()
    {
        // Arrange
        string json = """
        {
            "contentType": "image/png"
        }
        """;

        // Act
        FileInformationReadDto? dto = JsonSerializer.Deserialize<FileInformationReadDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Assert
        _ = dto.Should().NotBeNull();
        _ = dto!.ContentType.Should().Be("image/png");
        _ = dto.AllowedOrigins.Should().Be("*");
        _ = dto.Metadata.Size.Should().Be(0);
    }
}
