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
        FileInformationDescriptorDto descriptor = new();

        // Assert
        _ = descriptor.Url.Should().BeNull();
        _ = descriptor.Name.Should().BeNull();
        _ = descriptor.Owner.Should().BeNull();
        _ = descriptor.Checksum.Should().BeNull();
        _ = descriptor.Tag.Should().BeNull();
    }

    [Fact]
    public void FileInformationDescriptorDto_PropertiesAreSettable()
    {
        // Arrange
        FileInformationDescriptorDto descriptor = new()
        {
            // Act
            Url = "https://example.com/file.png",
            Name = "file.png",
            Owner = "tenant-123",
            Checksum = "abc123",
            Tag = "profile-picture"
        };

        // Assert
        _ = descriptor.Url.Should().Be("https://example.com/file.png");
        _ = descriptor.Name.Should().Be("file.png");
        _ = descriptor.Owner.Should().Be("tenant-123");
        _ = descriptor.Checksum.Should().Be("abc123");
        _ = descriptor.Tag.Should().Be("profile-picture");
    }

    [Fact]
    public void FileInformationDescriptorDto_WithInitializer_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        FileInformationDescriptorDto descriptor = new()
        {
            Url = "https://example.com/document.pdf",
            Name = "document.pdf",
            Owner = "tenant-456",
            Checksum = "def456",
            Tag = "document"
        };

        // Assert
        _ = descriptor.Url.Should().Be("https://example.com/document.pdf");
        _ = descriptor.Name.Should().Be("document.pdf");
        _ = descriptor.Owner.Should().Be("tenant-456");
        _ = descriptor.Checksum.Should().Be("def456");
        _ = descriptor.Tag.Should().Be("document");
    }

    [Fact]
    public void FileInformationDescriptorDto_Equality_WorksCorrectly()
    {
        // Arrange
        FileInformationDescriptorDto descriptor1 = new()
        {
            Checksum = "abc123",
            Owner = "tenant-1"
        };
        FileInformationDescriptorDto descriptor2 = new()
        {
            Checksum = "abc123",
            Owner = "tenant-1"
        };

        // Assert
        _ = descriptor1.Should().Be(descriptor2);
    }

    [Fact]
    public void FileInformationDescriptorDto_Inequality_WhenChecksumDiffers()
    {
        // Arrange
        FileInformationDescriptorDto descriptor1 = new()
        { Checksum = "abc123" };
        FileInformationDescriptorDto descriptor2 = new()
        { Checksum = "def456" };

        // Assert
        _ = descriptor1.Should().NotBe(descriptor2);
    }

    [Fact]
    public void FileInformationDescriptorDto_Inequality_WhenOwnerDiffers()
    {
        // Arrange
        FileInformationDescriptorDto descriptor1 = new()
        { Owner = "tenant-1" };
        FileInformationDescriptorDto descriptor2 = new()
        { Owner = "tenant-2" };

        // Assert
        _ = descriptor1.Should().NotBe(descriptor2);
    }

    [Fact]
    public void FileInformationDescriptorDto_WithCopy_CreatesNewInstance()
    {
        // Arrange
        FileInformationDescriptorDto original = new()
        {
            Checksum = "abc123",
            Owner = "tenant-1"
        };

        // Act
        FileInformationDescriptorDto copy = original with { Owner = "tenant-2" };

        // Assert
        _ = copy.Checksum.Should().Be("abc123");
        _ = copy.Owner.Should().Be("tenant-2");
        _ = original.Owner.Should().Be("tenant-1");
    }

    [Theory]
    [InlineData("abc123")]
    [InlineData("ABC123")]
    [InlineData("a1b2c3d4e5f6")]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef")]
    public void FileInformationDescriptorDto_AcceptsVariousChecksumFormats(string checksum)
    {
        // Arrange & Act
        FileInformationDescriptorDto descriptor = new()
        {
            Checksum = checksum
        };

        // Assert
        _ = descriptor.Checksum.Should().Be(checksum);
    }

    [Theory]
    [InlineData("file.png")]
    [InlineData("document.pdf")]
    [InlineData("video.mp4")]
    [InlineData("file with spaces.txt")]
    public void FileInformationDescriptorDto_AcceptsVariousNameFormats(string name)
    {
        // Arrange & Act
        FileInformationDescriptorDto descriptor = new()
        {
            Name = name
        };

        // Assert
        _ = descriptor.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("profile")]
    [InlineData("document")]
    [InlineData("avatar")]
    [InlineData("product-image")]
    public void FileInformationDescriptorDto_AcceptsVariousTags(string tag)
    {
        // Arrange & Act
        FileInformationDescriptorDto descriptor = new()
        {
            Tag = tag
        };

        // Assert
        _ = descriptor.Tag.Should().Be(tag);
    }
}
