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
        var dto = new FileInformationReadDto();

        // Assert
        dto.AllowedOrigins.Should().Be("*");
        dto.ContentType.Should().BeEmpty();
        dto.Thumbnail.Should().BeNull();
        dto.Checksum.Should().BeNull();
        dto.Url.Should().BeNull();
        dto.Owner.Should().BeNull();
        dto.Tag.Should().BeNull();
    }

    [Fact]
    public void FileInformationDto_PropertiesAreSettable()
    {
        // Arrange
        var dto = new FileInformationReadDto();

        // Act
        dto.AllowedOrigins = "https://example.com";
        dto.ContentType = "image/png";
        dto.Thumbnail = "https://example.com/thumb.png";
        dto.Checksum = "abc123";
        dto.Url = "https://example.com/file.png";
        dto.Owner = "tenant-1";
        dto.Tag = "profile";

        // Assert
        dto.AllowedOrigins.Should().Be("https://example.com");
        dto.ContentType.Should().Be("image/png");
        dto.Thumbnail.Should().Be("https://example.com/thumb.png");
        dto.Checksum.Should().Be("abc123");
        dto.Url.Should().Be("https://example.com/file.png");
        dto.Owner.Should().Be("tenant-1");
        dto.Tag.Should().Be("profile");
    }

    [Fact]
    public void Metadata_WhenNotSet_ReturnsEmptyFileMetadata()
    {
        // Arrange
        var dto = new FileInformationReadDto();

        // Act
        FileMetadata metadata = dto.Metadata;

        // Assert
        metadata.Should().NotBeNull();
        metadata.Size.Should().Be(0);
        metadata.Extension.Should().BeNull();
        metadata.Dimensions.Should().BeNull();
    }

    [Fact]
    public void Metadata_WhenSet_ReturnsCorrectValue()
    {
        // Arrange
        var dto = new FileInformationReadDto();
        var expectedMetadata = new FileMetadata
        {
            Size = 1024,
            Extension = "png",
            Dimensions = new FileDimensions { Width = 100, Height = 100 }
        };

        // Act
        dto.Metadata = expectedMetadata;

        // Assert
        dto.Metadata.Should().NotBeNull();
        dto.Metadata.Size.Should().Be(1024);
        dto.Metadata.Extension.Should().Be("png");
        dto.Metadata.Dimensions!.Width.Should().Be(100);
        dto.Metadata.Dimensions.Height.Should().Be(100);
    }

    [Fact]
    public void Metadata_WhenSetToNull_ReturnsEmptyMetadata()
    {
        // Arrange
        var dto = new FileInformationReadDto
        {
            Metadata = new FileMetadata { Size = 1024 }
        };

        // Act
        dto.Metadata = null!;

        // Assert
        dto.Metadata.Should().NotBeNull();
        dto.Metadata.Size.Should().Be(0);
    }

    [Fact]
    public void MetadataJson_WhenMetadataSet_ReturnsSerializedJson()
    {
        // Arrange
        var dto = new FileInformationReadDto
        {
            Metadata = new FileMetadata
            {
                Size = 2048,
                Extension = "jpg"
            }
        };

        // Act
        var json = dto.MetadataJson;

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("2048");
        json.Should().Contain("jpg");
    }

    [Fact]
    public void MetadataJson_WhenSet_UpdatesMetadataProperty()
    {
        // Arrange
        var dto = new FileInformationReadDto();
        var metadataJson = """{"size":4096,"extension":"gif"}""";

        // Act
        dto.MetadataJson = metadataJson;
        FileMetadata metadata = dto.Metadata;

        // Assert
        metadata.Size.Should().Be(4096);
        metadata.Extension.Should().Be("gif");
    }

    [Fact]
    public void MetadataJson_WhenSetWithDimensions_UpdatesMetadataProperty()
    {
        // Arrange
        var dto = new FileInformationReadDto();
        var metadataJson = """{"size":1024,"extension":"png","dimensions":{"width":800,"height":600}}""";

        // Act
        dto.MetadataJson = metadataJson;
        FileMetadata metadata = dto.Metadata;

        // Assert
        metadata.Size.Should().Be(1024);
        metadata.Extension.Should().Be("png");
        metadata.Dimensions.Should().NotBeNull();
        metadata.Dimensions!.Width.Should().Be(800);
        metadata.Dimensions.Height.Should().Be(600);
    }

    [Fact]
    public void MetadataJson_WhenSetWithInvalidJson_ReturnsEmptyMetadata()
    {
        // Arrange
        var dto = new FileInformationReadDto();
        var invalidJson = "not valid json";

        // Act
        dto.MetadataJson = invalidJson;
        FileMetadata metadata = dto.Metadata;

        // Assert
        metadata.Should().NotBeNull();
        metadata.Size.Should().Be(0);
        metadata.Extension.Should().BeNull();
    }

    [Fact]
    public void MetadataJson_WhenSetWithEmptyString_ReturnsEmptyMetadata()
    {
        // Arrange
        var dto = new FileInformationReadDto();

        // Act
        dto.MetadataJson = "";
        FileMetadata metadata = dto.Metadata;

        // Assert
        metadata.Should().NotBeNull();
        metadata.Size.Should().Be(0);
    }

    [Fact]
    public void MetadataJson_WhenSetWithWhitespace_ReturnsEmptyMetadata()
    {
        // Arrange
        var dto = new FileInformationReadDto();

        // Act
        dto.MetadataJson = "   ";
        FileMetadata metadata = dto.Metadata;

        // Assert
        metadata.Should().NotBeNull();
        metadata.Size.Should().Be(0);
    }

    [Fact]
    public void MetadataJson_Caching_ReturnsConsistentValue()
    {
        // Arrange
        var dto = new FileInformationReadDto
        {
            Metadata = new FileMetadata { Size = 1024, Extension = "png" }
        };

        // Act
        var json1 = dto.MetadataJson;
        var json2 = dto.MetadataJson;

        // Assert
        json1.Should().Be(json2);
    }

    [Fact]
    public void MetadataJson_WhenMetadataUpdated_CacheInvalidated()
    {
        // Arrange
        var dto = new FileInformationReadDto
        {
            Metadata = new FileMetadata { Size = 1024 }
        };
        var originalJson = dto.MetadataJson;

        // Act
        dto.Metadata = new FileMetadata { Size = 2048 };
        var newJson = dto.MetadataJson;

        // Assert
        originalJson.Should().NotBe(newJson);
        newJson.Should().Contain("2048");
    }

    [Fact]
    public void Metadata_WhenMetadataJsonSet_PreviousMetadataCleared()
    {
        // Arrange
        var dto = new FileInformationReadDto
        {
            Metadata = new FileMetadata { Size = 1024, Extension = "png" }
        };

        // Act
        dto.MetadataJson = """{"size":512,"extension":"jpg"}""";
        FileMetadata metadata = dto.Metadata;

        // Assert
        metadata.Size.Should().Be(512);
        metadata.Extension.Should().Be("jpg");
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://example.com")]
    [InlineData("https://app.example.com,https://api.example.com")]
    public void FileInformationDto_AcceptsVariousAllowedOrigins(string allowedOrigins)
    {
        // Arrange & Act
        var dto = new FileInformationReadDto
        {
            AllowedOrigins = allowedOrigins
        };

        // Assert
        dto.AllowedOrigins.Should().Be(allowedOrigins);
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
        var dto = new FileInformationReadDto
        {
            ContentType = contentType
        };

        // Assert
        dto.ContentType.Should().Be(contentType);
    }

    [Fact]
    public void FileInformationDto_JsonSerialization_IgnoresMetadataJson()
    {
        // Arrange
        var dto = new FileInformationReadDto
        {
            ContentType = "image/png",
            Metadata = new FileMetadata { Size = 1024, Extension = "png" }
        };

        // Act
        var json = JsonSerializer.Serialize(dto);

        // Assert
        json.Should().NotContain("metadataJson");
        json.Should().Contain("metadata");
    }

    [Fact]
    public void FileInformationDto_JsonDeserialization_WorksCorrectly()
    {
        // Arrange
        var json = """
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
        dto.Should().NotBeNull();
        dto!.AllowedOrigins.Should().Be("https://example.com");
        dto.ContentType.Should().Be("image/png");
        dto.Thumbnail.Should().Be("https://example.com/thumb.png");
        dto.Checksum.Should().Be("abc123");
        dto.Url.Should().Be("https://example.com/file.png");
        dto.Owner.Should().Be("tenant-1");
        dto.Metadata.Size.Should().Be(1024);
        dto.Metadata.Extension.Should().Be("png");
        dto.Tag.Should().Be("profile");
    }

    [Fact]
    public void FileInformationDto_PartialJsonDeserialization_WorksCorrectly()
    {
        // Arrange
        var json = """
        {
            "contentType": "image/png"
        }
        """;

        // Act
        FileInformationReadDto? dto = JsonSerializer.Deserialize<FileInformationReadDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        // Assert
        dto.Should().NotBeNull();
        dto!.ContentType.Should().Be("image/png");
        dto.AllowedOrigins.Should().Be("*");
        dto.Metadata.Size.Should().Be(0);
    }
}
