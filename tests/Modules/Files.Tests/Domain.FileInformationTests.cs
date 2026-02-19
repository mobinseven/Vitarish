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
        var fileInfo = new FileInformation
        {
            ContentType = "image/png"
        };

        // Assert
        fileInfo.ContentType.Should().Be("image/png");
        fileInfo.AllowedOrigins.Should().Be("*");
        fileInfo.Thumbnail.Should().BeEmpty();
        fileInfo.Extension.Should().BeEmpty();
        fileInfo.Size.Should().Be(0);
        fileInfo.Checksum.Should().BeEmpty();
        fileInfo.Url.Should().BeEmpty();
        fileInfo.Metadata.Should().BeNull();
        fileInfo.TenantId.Should().BeNull();
        fileInfo.Tag.Should().BeNull();
    }

    [Fact]
    public void FileInformation_PropertiesAreSettable()
    {
        // Arrange
        var fileInfo = new FileInformation
        {
            ContentType = "image/png"
        };

        // Act
        fileInfo.AllowedOrigins = "https://example.com";
        fileInfo.Thumbnail = "https://example.com/thumb.png";
        fileInfo.Checksum = "abc123";
        fileInfo.Url = "https://example.com/file.png";
        fileInfo.TenantId = "tenant-1";
        fileInfo.Tag = "profile-picture";

        // Assert
        fileInfo.AllowedOrigins.Should().Be("https://example.com");
        fileInfo.Thumbnail.Should().Be("https://example.com/thumb.png");
        fileInfo.Checksum.Should().Be("abc123");
        fileInfo.Url.Should().Be("https://example.com/file.png");
        fileInfo.TenantId.Should().Be("tenant-1");
        fileInfo.Tag.Should().Be("profile-picture");
    }

    [Fact]
    public void Owner_ReturnsAndSetsTenantId()
    {
        // Arrange
        var fileInfo = new FileInformation
        {
            ContentType = "application/pdf"
        };

        // Act - Set via Owner
        fileInfo.Owner = "tenant-123";

        // Assert
        fileInfo.Owner.Should().Be("tenant-123");
        fileInfo.TenantId.Should().Be("tenant-123");
    }

    [Fact]
    public void Owner_WhenTenantIdSet_ReturnsCorrectValue()
    {
        // Arrange
        var fileInfo = new FileInformation
        {
            ContentType = "application/pdf",
            TenantId = "tenant-456"
        };

        // Assert
        fileInfo.Owner.Should().Be("tenant-456");
    }

    [Fact]
    public void Metadata_WhenSetWithValidJson_AppliesMetadataValues()
    {
        // Arrange
        var fileInfo = new FileInformation
        {
            ContentType = "image/jpeg"
        };
        var metadataJson = """{"size":1024,"extension":"jpg","dimensions":{"width":800,"height":600}}""";

        // Act
        fileInfo.Metadata = metadataJson;

        // Assert
        fileInfo.Extension.Should().Be("jpg");
        fileInfo.Size.Should().Be(1024);
    }

    [Fact]
    public void Metadata_WhenSetWithPartialJson_AppliesAvailableValues()
    {
        // Arrange
        var fileInfo = new FileInformation
        {
            ContentType = "image/gif"
        };
        var metadataJson = """{"size":512}""";

        // Act
        fileInfo.Metadata = metadataJson;

        // Assert
        fileInfo.Size.Should().Be(512);
        fileInfo.Extension.Should().BeEmpty();
    }

    [Fact]
    public void Metadata_WhenSetWithNullOrEmpty_ResetsValues()
    {
        // Arrange
        var fileInfo = new FileInformation
        {
            ContentType = "image/png",
            Metadata = """{"size":2048,"extension":"png"}"""
        };

        // Act
        fileInfo.Metadata = null;

        // Assert
        fileInfo.Extension.Should().BeEmpty();
        fileInfo.Size.Should().Be(0);
    }

    [Fact]
    public void Metadata_WhenSetWithEmptyString_ResetsValues()
    {
        // Arrange
        var fileInfo = new FileInformation
        {
            ContentType = "image/png",
            Metadata = """{"size":2048,"extension":"png"}"""
        };

        // Act
        fileInfo.Metadata = "";

        // Assert
        fileInfo.Extension.Should().BeEmpty();
        fileInfo.Size.Should().Be(0);
    }

    [Fact]
    public void Metadata_WhenSetWithWhitespace_ResetsValues()
    {
        // Arrange
        var fileInfo = new FileInformation
        {
            ContentType = "image/png",
            Metadata = """{"size":2048,"extension":"png"}"""
        };

        // Act
        fileInfo.Metadata = "   ";

        // Assert
        fileInfo.Extension.Should().BeEmpty();
        fileInfo.Size.Should().Be(0);
    }

    [Fact]
    public void Metadata_WhenSetWithInvalidJson_ResetsValues()
    {
        // Arrange
        var fileInfo = new FileInformation
        {
            ContentType = "image/png"
        };
        var invalidJson = "not valid json";

        // Act
        fileInfo.Metadata = invalidJson;

        // Assert
        fileInfo.Extension.Should().BeEmpty();
        fileInfo.Size.Should().Be(0);
    }

    [Fact]
    public void Metadata_WhenSetWithMalformedJson_ResetsValues()
    {
        // Arrange
        var fileInfo = new FileInformation
        {
            ContentType = "image/png"
        };
        var malformedJson = """{"size": "not a number"}""";

        // Act
        fileInfo.Metadata = malformedJson;

        // Assert
        fileInfo.Size.Should().Be(0);
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
        var fileInfo = new FileInformation
        {
            ContentType = contentType
        };

        // Assert
        fileInfo.ContentType.Should().Be(contentType);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://example.com")]
    [InlineData("https://app.example.com,https://api.example.com")]
    [InlineData("http://localhost:3000")]
    public void FileInformation_AcceptsVariousAllowedOrigins(string allowedOrigins)
    {
        // Arrange & Act
        var fileInfo = new FileInformation
        {
            ContentType = "image/png",
            AllowedOrigins = allowedOrigins
        };

        // Assert
        fileInfo.AllowedOrigins.Should().Be(allowedOrigins);
    }

    [Fact]
    public void Metadata_WhenExtensionOnlyProvided_AppliesExtension()
    {
        // Arrange
        var fileInfo = new FileInformation
        {
            ContentType = "image/png"
        };
        var metadataJson = """{"extension":"png"}""";

        // Act
        fileInfo.Metadata = metadataJson;

        // Assert
        fileInfo.Extension.Should().Be("png");
        fileInfo.Size.Should().Be(0);
    }

    [Fact]
    public void Metadata_WhenDimensionsProvided_AppliesCorrectly()
    {
        // Arrange
        var fileInfo = new FileInformation
        {
            ContentType = "image/png"
        };
        var metadataJson = """{"size":4096,"extension":"png","dimensions":{"width":1920,"height":1080}}""";

        // Act
        fileInfo.Metadata = metadataJson;

        // Assert
        fileInfo.Extension.Should().Be("png");
        fileInfo.Size.Should().Be(4096);
    }
}
