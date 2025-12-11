using Vitarish.Files.Infrastructure.Extensions;

namespace Vitarish.Files.Tests;

/// <summary>
/// Unit tests for the <see cref="FilesExtensions"/> class.
/// </summary>
public sealed class FilesExtensionsTests
{
    [Fact]
    public void ExtractId_WithValidFileName_ReturnsId()
    {
        // Arrange
        var fileName = "files-abc123def456.png";
        var prefix = "files";

        // Act
        var result = FilesExtensions.ExtractId(fileName, prefix);

        // Assert
        result.Should().Be("abc123def456");
    }

    [Fact]
    public void ExtractId_WithGuidId_ReturnsGuid()
    {
        // Arrange
        var guid = Guid.NewGuid().ToString("N");
        var fileName = $"uploads-{guid}.jpg";
        var prefix = "uploads";

        // Act
        var result = FilesExtensions.ExtractId(fileName, prefix);

        // Assert
        result.Should().Be(guid);
    }

    [Fact]
    public void ExtractId_WithDifferentExtensions_ExtractsCorrectly()
    {
        // Arrange
        var id = "test-id-123";
        var prefix = "media";

        // Act & Assert
        FilesExtensions.ExtractId($"{prefix}-{id}.png", prefix).Should().Be(id);
        FilesExtensions.ExtractId($"{prefix}-{id}.jpg", prefix).Should().Be(id);
        FilesExtensions.ExtractId($"{prefix}-{id}.pdf", prefix).Should().Be(id);
        FilesExtensions.ExtractId($"{prefix}-{id}.mp4", prefix).Should().Be(id);
    }

    [Fact]
    public void ExtractId_WithNullFileName_ThrowsArgumentNullException()
    {
        // Arrange
        string fileName = null!;
        var prefix = "files";

        // Act
        var act = () => FilesExtensions.ExtractId(fileName, prefix);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("fileName");
    }

    [Fact]
    public void ExtractId_WithNullPrefix_ThrowsArgumentNullException()
    {
        // Arrange
        var fileName = "files-abc123.png";
        string prefix = null!;

        // Act
        var act = () => FilesExtensions.ExtractId(fileName, prefix);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("prefix");
    }

    [Fact]
    public void ExtractId_WithWrongPrefix_ThrowsFormatException()
    {
        // Arrange
        var fileName = "files-abc123.png";
        var prefix = "uploads"; // Wrong prefix

        // Act
        var act = () => FilesExtensions.ExtractId(fileName, prefix);

        // Assert
        act.Should().Throw<FormatException>()
           .WithMessage("*prefix*");
    }

    [Fact]
    public void ExtractId_WithNoExtension_ThrowsFormatException()
    {
        // Arrange
        var fileName = "files-abc123"; // No extension
        var prefix = "files";

        // Act
        var act = () => FilesExtensions.ExtractId(fileName, prefix);

        // Assert
        act.Should().Throw<FormatException>()
           .WithMessage("*extension*");
    }

    [Fact]
    public void ExtractId_CaseInsensitivePrefix()
    {
        // Arrange
        var fileName = "FILES-abc123.png";
        var prefix = "files";

        // Act
        var result = FilesExtensions.ExtractId(fileName, prefix);

        // Assert
        result.Should().Be("abc123");
    }

    [Fact]
    public void ExtractId_WithMultipleDots_ExtractsIdBeforeFirstDot()
    {
        // Arrange - ExtractId returns everything between prefix and first dot
        var fileName = "files-abc.123.def.png";
        var prefix = "files";

        // Act
        var result = FilesExtensions.ExtractId(fileName, prefix);

        // Assert - Only returns the ID portion before the first dot
        result.Should().Be("abc");
    }
}

/// <summary>
/// Unit tests for the <see cref="ObjectStorageExtensions"/> class.
/// </summary>
public sealed class ObjectStorageExtensionsTests
{
    [Fact]
    public void BuildObjectKey_WithUploadPath_ReturnsCorrectKey()
    {
        // Arrange
        var options = new FilesOptions
        {
            UploadPath = "uploads"
        };
        var fileName = "test-file.png";

        // Act
        var result = options.BuildObjectKey(fileName);

        // Assert
        result.Should().Be("uploads/test-file.png");
    }

    [Fact]
    public void BuildObjectKey_WithoutUploadPath_ReturnsFileNameOnly()
    {
        // Arrange
        var options = new FilesOptions
        {
            UploadPath = ""
        };
        var fileName = "test-file.png";

        // Act
        var result = options.BuildObjectKey(fileName);

        // Assert
        result.Should().Be("test-file.png");
    }

    [Fact]
    public void BuildObjectKey_WithNullUploadPath_ReturnsFileNameOnly()
    {
        // Arrange
        var options = new FilesOptions
        {
            UploadPath = null!
        };
        var fileName = "test-file.png";

        // Act
        var result = options.BuildObjectKey(fileName);

        // Assert
        result.Should().Be("test-file.png");
    }

    [Fact]
    public void BuildObjectKey_TrimsLeadingSlashFromFileName()
    {
        // Arrange
        var options = new FilesOptions
        {
            UploadPath = "uploads"
        };
        var fileName = "/test-file.png";

        // Act
        var result = options.BuildObjectKey(fileName);

        // Assert
        result.Should().Be("uploads/test-file.png");
    }

    [Fact]
    public void BuildObjectKey_TrimsSlashesFromUploadPath()
    {
        // Arrange
        var options = new FilesOptions
        {
            UploadPath = "/uploads/"
        };
        var fileName = "test-file.png";

        // Act
        var result = options.BuildObjectKey(fileName);

        // Assert
        result.Should().Be("uploads/test-file.png");
    }

    [Fact]
    public void BuildObjectKey_WithNestedUploadPath_ReturnsCorrectKey()
    {
        // Arrange
        var options = new FilesOptions
        {
            UploadPath = "media/images"
        };
        var fileName = "test-file.png";

        // Act
        var result = options.BuildObjectKey(fileName);

        // Assert
        result.Should().Be("media/images/test-file.png");
    }

    [Theory]
    [InlineData("uploads", "file.png", "uploads/file.png")]
    [InlineData("media", "image.jpg", "media/image.jpg")]
    [InlineData("docs/pdf", "document.pdf", "docs/pdf/document.pdf")]
    [InlineData("", "file.txt", "file.txt")]
    public void BuildObjectKey_VariousCombinations_ReturnsExpectedResults(string uploadPath, string fileName, string expected)
    {
        // Arrange
        var options = new FilesOptions
        {
            UploadPath = uploadPath
        };

        // Act
        var result = options.BuildObjectKey(fileName);

        // Assert
        result.Should().Be(expected);
    }
}

/// <summary>
/// Unit tests for the ContentTypeExtensions class.
/// </summary>
public sealed class ContentTypeExtensionsTests
{
    [Theory]
    [InlineData("image/png", "png")]
    [InlineData("image/jpeg", "jpeg")]
    [InlineData("image/gif", "gif")]
    [InlineData("application/pdf", "pdf")]
    [InlineData("video/mp4", "mp4")]
    [InlineData("text/plain", "txt")]
    public void SafeGetExtension_WithKnownMimeTypes_ReturnsCorrectExtension(string mimeType, string expectedExtension)
    {
        // Act
        var result = ContentTypeExtensions.SafeGetExtension(mimeType);

        // Assert
        result.Should().Be(expectedExtension);
    }

    [Fact]
    public void SafeGetExtension_WithUnknownMimeType_ReturnsBin()
    {
        // Arrange
        var unknownMimeType = "application/x-unknown-type-12345";

        // Act
        var result = ContentTypeExtensions.SafeGetExtension(unknownMimeType);

        // Assert
        result.Should().Be("bin");
    }

    [Fact]
    public void SafeGetExtension_WithEmptyMimeType_DoesNotThrow()
    {
        // Act - the library may return unexpected values for empty strings,
        // but we just want to ensure it doesn't throw and returns a non-null result
        var result = ContentTypeExtensions.SafeGetExtension("");

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData("png", "image/png")]
    [InlineData("jpg", "image/jpeg")]
    [InlineData("gif", "image/gif")]
    [InlineData("pdf", "application/pdf")]
    [InlineData("mp4", "video/mp4")]
    [InlineData("txt", "text/plain")]
    public void SafeGetContentType_WithKnownExtensions_ReturnsCorrectMimeType(string extension, string expectedMimeType)
    {
        // Act
        var result = ContentTypeExtensions.SafeGetContentType(extension);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Theory]
    [InlineData(".png", "image/png")]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".PDF", "application/pdf")]
    public void SafeGetContentType_WithLeadingDotAndVariousCase_ReturnsCorrectMimeType(string extension, string expectedMimeType)
    {
        // Act
        var result = ContentTypeExtensions.SafeGetContentType(extension);

        // Assert
        result.Should().Be(expectedMimeType);
    }

    [Fact]
    public void SafeGetContentType_WithUnknownExtension_ReturnsFallback()
    {
        // Arrange
        var unknownExtension = "xyz12345";

        // Act
        var result = ContentTypeExtensions.SafeGetContentType(unknownExtension);

        // Assert
        result.Should().Be("application/octet-stream");
    }

    [Fact]
    public void SafeGetContentType_WithEmptyExtension_ReturnsFallback()
    {
        // Act
        var result = ContentTypeExtensions.SafeGetContentType("");

        // Assert
        result.Should().Be("application/octet-stream");
    }

    [Fact]
    public void SafeGetContentType_WithWhitespace_ReturnsFallback()
    {
        // Act
        var result = ContentTypeExtensions.SafeGetContentType("   ");

        // Assert
        result.Should().Be("application/octet-stream");
    }

    [Fact]
    public void SafeGetContentType_WithNull_ReturnsFallback()
    {
        // Act
        var result = ContentTypeExtensions.SafeGetContentType(null!);

        // Assert
        result.Should().Be("application/octet-stream");
    }
}
