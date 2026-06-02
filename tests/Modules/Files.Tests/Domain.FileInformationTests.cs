namespace Files.Tests;

/// <summary>
/// Unit tests for the <see cref="FileInformation"/> entity.
/// </summary>
public sealed class FileInformationTests
{
    [Fact]
    public void FileInformation_WithRequiredProperties_CreatesValidInstance()
    {
        // Arrange & Act
        FileInformation fileInfo = new()
        {
            ContentType = "image/png"
        };

        // Assert
        _ = fileInfo.ContentType.Should().Be("image/png");
        _ = fileInfo.AllowedOrigins.Should().Be("*");
        _ = fileInfo.Thumbnail.Should().BeEmpty();
        _ = fileInfo.Extension.Should().BeEmpty();
        _ = fileInfo.Size.Should().Be(0);
        _ = fileInfo.Checksum.Should().BeEmpty();
        _ = fileInfo.Url.Should().BeEmpty();
        _ = fileInfo.Metadata.Should().BeNull();
        _ = fileInfo.TenantId.Should().BeNull();
        _ = fileInfo.Tag.Should().BeNull();
    }

    [Fact]
    public void FileInformation_PropertiesAreSettable()
    {
        // Arrange
        FileInformation fileInfo = new()
        {
            ContentType = "image/png",
            // Act
            AllowedOrigins = "https://example.com",
            Thumbnail = "https://example.com/thumb.png",
            Checksum = "abc123",
            Url = "https://example.com/file.png",
            TenantId = "tenant-1",
            Tag = "profile-picture"
        };

        // Assert
        _ = fileInfo.AllowedOrigins.Should().Be("https://example.com");
        _ = fileInfo.Thumbnail.Should().Be("https://example.com/thumb.png");
        _ = fileInfo.Checksum.Should().Be("abc123");
        _ = fileInfo.Url.Should().Be("https://example.com/file.png");
        _ = fileInfo.TenantId.Should().Be("tenant-1");
        _ = fileInfo.Tag.Should().Be("profile-picture");
    }

    [Fact]
    public void Owner_ReturnsAndSetsTenantId()
    {
        // Arrange
        FileInformation fileInfo = new()
        {
            ContentType = "application/pdf",
            // Act - Set via Owner
            Owner = "tenant-123"
        };

        // Assert
        _ = fileInfo.Owner.Should().Be("tenant-123");
        _ = fileInfo.TenantId.Should().Be("tenant-123");
    }

    [Fact]
    public void Owner_WhenTenantIdSet_ReturnsCorrectValue()
    {
        // Arrange
        FileInformation fileInfo = new()
        {
            ContentType = "application/pdf",
            TenantId = "tenant-456"
        };

        // Assert
        _ = fileInfo.Owner.Should().Be("tenant-456");
    }

    [Fact]
    public void Metadata_WhenSetWithValidJson_AppliesMetadataValues()
    {
        // Arrange
        FileInformation fileInfo = new()
        {
            ContentType = "image/jpeg"
        };
        string metadataJson = """{"size":1024,"extension":"jpg","dimensions":{"width":800,"height":600}}""";

        // Act
        fileInfo.Metadata = metadataJson;

        // Assert
        _ = fileInfo.Extension.Should().Be("jpg");
        _ = fileInfo.Size.Should().Be(1024);
    }

    [Fact]
    public void Metadata_WhenSetWithPartialJson_AppliesAvailableValues()
    {
        // Arrange
        FileInformation fileInfo = new()
        {
            ContentType = "image/gif"
        };
        string metadataJson = """{"size":512}""";

        // Act
        fileInfo.Metadata = metadataJson;

        // Assert
        _ = fileInfo.Size.Should().Be(512);
        _ = fileInfo.Extension.Should().BeEmpty();
    }

    [Fact]
    public void Metadata_WhenSetWithNullOrEmpty_ResetsValues()
    {
        // Arrange
        FileInformation fileInfo = new()
        {
            ContentType = "image/png",
            Metadata = """{"size":2048,"extension":"png"}"""
        };

        // Act
        fileInfo.Metadata = null;

        // Assert
        _ = fileInfo.Extension.Should().BeEmpty();
        _ = fileInfo.Size.Should().Be(0);
    }

    [Fact]
    public void Metadata_WhenSetWithEmptyString_ResetsValues()
    {
        // Arrange
        FileInformation fileInfo = new()
        {
            ContentType = "image/png",
            Metadata = """{"size":2048,"extension":"png"}"""
        };

        // Act
        fileInfo.Metadata = "";

        // Assert
        _ = fileInfo.Extension.Should().BeEmpty();
        _ = fileInfo.Size.Should().Be(0);
    }

    [Fact]
    public void Metadata_WhenSetWithWhitespace_ResetsValues()
    {
        // Arrange
        FileInformation fileInfo = new()
        {
            ContentType = "image/png",
            Metadata = """{"size":2048,"extension":"png"}"""
        };

        // Act
        fileInfo.Metadata = "   ";

        // Assert
        _ = fileInfo.Extension.Should().BeEmpty();
        _ = fileInfo.Size.Should().Be(0);
    }

    [Fact]
    public void Metadata_WhenSetWithInvalidJson_ResetsValues()
    {
        // Arrange
        FileInformation fileInfo = new()
        {
            ContentType = "image/png"
        };
        string invalidJson = "not valid json";

        // Act
        fileInfo.Metadata = invalidJson;

        // Assert
        _ = fileInfo.Extension.Should().BeEmpty();
        _ = fileInfo.Size.Should().Be(0);
    }

    [Fact]
    public void Metadata_WhenSetWithMalformedJson_ResetsValues()
    {
        // Arrange
        FileInformation fileInfo = new()
        {
            ContentType = "image/png"
        };
        string malformedJson = """{"size": "not a number"}""";

        // Act
        fileInfo.Metadata = malformedJson;

        // Assert
        _ = fileInfo.Size.Should().Be(0);
    }

    [Theory]
    [InlineData("image/png")]
    [InlineData("image/jpeg")]
    [InlineData("application/pdf")]
    [InlineData("video/mp4")]
    [InlineData("application/octet-stream")]
    public void FileInformation_AcceptsVariousContentTypes(string contentType)
    {
        // Arrange & Act
        FileInformation fileInfo = new()
        {
            ContentType = contentType
        };

        // Assert
        _ = fileInfo.ContentType.Should().Be(contentType);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://example.com")]
    [InlineData("https://app.example.com,https://api.example.com")]
    [InlineData("http://localhost:3000")]
    public void FileInformation_AcceptsVariousAllowedOrigins(string allowedOrigins)
    {
        // Arrange & Act
        FileInformation fileInfo = new()
        {
            ContentType = "image/png",
            AllowedOrigins = allowedOrigins
        };

        // Assert
        _ = fileInfo.AllowedOrigins.Should().Be(allowedOrigins);
    }

    [Fact]
    public void Metadata_WhenExtensionOnlyProvided_AppliesExtension()
    {
        // Arrange
        FileInformation fileInfo = new()
        {
            ContentType = "image/png"
        };
        string metadataJson = """{"extension":"png"}""";

        // Act
        fileInfo.Metadata = metadataJson;

        // Assert
        _ = fileInfo.Extension.Should().Be("png");
        _ = fileInfo.Size.Should().Be(0);
    }

    [Fact]
    public void Metadata_WhenDimensionsProvided_AppliesCorrectly()
    {
        // Arrange
        FileInformation fileInfo = new()
        {
            ContentType = "image/png"
        };
        string metadataJson = """{"size":4096,"extension":"png","dimensions":{"width":1920,"height":1080}}""";

        // Act
        fileInfo.Metadata = metadataJson;

        // Assert
        _ = fileInfo.Extension.Should().Be("png");
        _ = fileInfo.Size.Should().Be(4096);
    }
}
