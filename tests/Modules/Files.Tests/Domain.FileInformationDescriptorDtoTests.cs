namespace Files.Tests;

/// <summary>
/// Unit tests for the <see cref="FileInformationDescriptorDto"/> record.
/// </summary>
public sealed class FileInformationDescriptorDtoTests
{
    [Fact]
    public void FileInformationDescriptorDto_DefaultValues_AreNull()
    {
        // Arrange & Act
        var descriptor = new FileInformationDescriptorDto();

        // Assert
        descriptor.Url.Should().BeNull();
        descriptor.Name.Should().BeNull();
        descriptor.Owner.Should().BeNull();
        descriptor.Checksum.Should().BeNull();
        descriptor.Tag.Should().BeNull();
    }

    [Fact]
    public void FileInformationDescriptorDto_PropertiesAreSettable()
    {
        // Arrange
        var descriptor = new FileInformationDescriptorDto();

        // Act
        descriptor.Url = "https://example.com/file.png";
        descriptor.Name = "file.png";
        descriptor.Owner = "tenant-123";
        descriptor.Checksum = "abc123";
        descriptor.Tag = "profile-picture";

        // Assert
        descriptor.Url.Should().Be("https://example.com/file.png");
        descriptor.Name.Should().Be("file.png");
        descriptor.Owner.Should().Be("tenant-123");
        descriptor.Checksum.Should().Be("abc123");
        descriptor.Tag.Should().Be("profile-picture");
    }

    [Fact]
    public void FileInformationDescriptorDto_WithInitializer_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var descriptor = new FileInformationDescriptorDto
        {
            Url = "https://example.com/document.pdf",
            Name = "document.pdf",
            Owner = "tenant-456",
            Checksum = "def456",
            Tag = "document"
        };

        // Assert
        descriptor.Url.Should().Be("https://example.com/document.pdf");
        descriptor.Name.Should().Be("document.pdf");
        descriptor.Owner.Should().Be("tenant-456");
        descriptor.Checksum.Should().Be("def456");
        descriptor.Tag.Should().Be("document");
    }

    [Fact]
    public void FileInformationDescriptorDto_Equality_WorksCorrectly()
    {
        // Arrange
        var descriptor1 = new FileInformationDescriptorDto
        {
            Checksum = "abc123",
            Owner = "tenant-1"
        };
        var descriptor2 = new FileInformationDescriptorDto
        {
            Checksum = "abc123",
            Owner = "tenant-1"
        };

        // Assert
        descriptor1.Should().Be(descriptor2);
    }

    [Fact]
    public void FileInformationDescriptorDto_Inequality_WhenChecksumDiffers()
    {
        // Arrange
        var descriptor1 = new FileInformationDescriptorDto { Checksum = "abc123" };
        var descriptor2 = new FileInformationDescriptorDto { Checksum = "def456" };

        // Assert
        descriptor1.Should().NotBe(descriptor2);
    }

    [Fact]
    public void FileInformationDescriptorDto_Inequality_WhenOwnerDiffers()
    {
        // Arrange
        var descriptor1 = new FileInformationDescriptorDto { Owner = "tenant-1" };
        var descriptor2 = new FileInformationDescriptorDto { Owner = "tenant-2" };

        // Assert
        descriptor1.Should().NotBe(descriptor2);
    }

    [Fact]
    public void FileInformationDescriptorDto_WithCopy_CreatesNewInstance()
    {
        // Arrange
        var original = new FileInformationDescriptorDto
        {
            Checksum = "abc123",
            Owner = "tenant-1"
        };

        // Act
        FileInformationDescriptorDto copy = original with { Owner = "tenant-2" };

        // Assert
        copy.Checksum.Should().Be("abc123");
        copy.Owner.Should().Be("tenant-2");
        original.Owner.Should().Be("tenant-1");
    }

    [Theory]
    [InlineData("abc123")]
    [InlineData("ABC123")]
    [InlineData("a1b2c3d4e5f6")]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef")]
    public void FileInformationDescriptorDto_AcceptsVariousChecksumFormats(string checksum)
    {
        // Arrange & Act
        var descriptor = new FileInformationDescriptorDto
        {
            Checksum = checksum
        };

        // Assert
        descriptor.Checksum.Should().Be(checksum);
    }

    [Theory]
    [InlineData("file.png")]
    [InlineData("document.pdf")]
    [InlineData("video.mp4")]
    [InlineData("file with spaces.txt")]
    public void FileInformationDescriptorDto_AcceptsVariousNameFormats(string name)
    {
        // Arrange & Act
        var descriptor = new FileInformationDescriptorDto
        {
            Name = name
        };

        // Assert
        descriptor.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("profile")]
    [InlineData("document")]
    [InlineData("avatar")]
    [InlineData("product-image")]
    public void FileInformationDescriptorDto_AcceptsVariousTags(string tag)
    {
        // Arrange & Act
        var descriptor = new FileInformationDescriptorDto
        {
            Tag = tag
        };

        // Assert
        descriptor.Tag.Should().Be(tag);
    }
}
