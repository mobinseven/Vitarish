namespace Files.Tests;

/// <summary>
/// Unit tests for the <see cref="FileMetadata"/> record.
/// </summary>
public sealed class FileMetadataTests
{
    [Fact]
    public void FileMetadata_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        FileMetadata metadata = new();

        // Assert
        _ = metadata.Size.Should().Be(0);
        _ = metadata.Extension.Should().BeNull();
        _ = metadata.Dimensions.Should().BeNull();
    }

    [Fact]
    public void FileMetadata_WithSize_SetsCorrectly()
    {
        // Arrange & Act
        FileMetadata metadata = new()
        {
            Size = 1024
        };

        // Assert
        _ = metadata.Size.Should().Be(1024);
    }

    [Fact]
    public void FileMetadata_WithExtension_SetsCorrectly()
    {
        // Arrange & Act
        FileMetadata metadata = new()
        {
            Extension = "png"
        };

        // Assert
        _ = metadata.Extension.Should().Be("png");
    }

    [Fact]
    public void FileMetadata_WithDimensions_SetsCorrectly()
    {
        // Arrange
        FileDimensions dimensions = new()
        {
            Width = 1920,
            Height = 1080
        };

        // Act
        FileMetadata metadata = new()
        {
            Dimensions = dimensions
        };

        // Assert
        _ = metadata.Dimensions.Should().NotBeNull();
        _ = metadata.Dimensions!.Width.Should().Be(1920);
        _ = metadata.Dimensions.Height.Should().Be(1080);
    }

    [Fact]
    public void FileMetadata_WithAllProperties_SetsCorrectly()
    {
        // Arrange
        FileDimensions dimensions = new()
        {
            Width = 800,
            Height = 600
        };

        // Act
        FileMetadata metadata = new()
        {
            Size = 2048,
            Extension = "jpg",
            Dimensions = dimensions
        };

        // Assert
        _ = metadata.Size.Should().Be(2048);
        _ = metadata.Extension.Should().Be("jpg");
        _ = metadata.Dimensions.Should().NotBeNull();
        _ = metadata.Dimensions!.Width.Should().Be(800);
        _ = metadata.Dimensions.Height.Should().Be(600);
    }

    [Fact]
    public void FileMetadata_Equality_WorksCorrectly()
    {
        // Arrange
        FileMetadata metadata1 = new()
        {
            Size = 1024,
            Extension = "png"
        };
        FileMetadata metadata2 = new()
        {
            Size = 1024,
            Extension = "png"
        };

        // Assert
        _ = metadata1.Should().Be(metadata2);
    }

    [Fact]
    public void FileMetadata_Equality_WithDimensions_WorksCorrectly()
    {
        // Arrange
        FileMetadata metadata1 = new()
        {
            Size = 1024,
            Extension = "png",
            Dimensions = new FileDimensions { Width = 100, Height = 100 }
        };
        FileMetadata metadata2 = new()
        {
            Size = 1024,
            Extension = "png",
            Dimensions = new FileDimensions { Width = 100, Height = 100 }
        };

        // Assert
        _ = metadata1.Should().Be(metadata2);
    }

    [Fact]
    public void FileMetadata_Inequality_WhenSizeDiffers()
    {
        // Arrange
        FileMetadata metadata1 = new()
        { Size = 1024 };
        FileMetadata metadata2 = new()
        { Size = 2048 };

        // Assert
        _ = metadata1.Should().NotBe(metadata2);
    }

    [Fact]
    public void FileMetadata_Inequality_WhenExtensionDiffers()
    {
        // Arrange
        FileMetadata metadata1 = new()
        { Extension = "png" };
        FileMetadata metadata2 = new()
        { Extension = "jpg" };

        // Assert
        _ = metadata1.Should().NotBe(metadata2);
    }

    [Fact]
    public void FileMetadata_Inequality_WhenDimensionsDiffer()
    {
        // Arrange
        FileMetadata metadata1 = new()
        {
            Dimensions = new FileDimensions { Width = 100, Height = 100 }
        };
        FileMetadata metadata2 = new()
        {
            Dimensions = new FileDimensions { Width = 200, Height = 200 }
        };

        // Assert
        _ = metadata1.Should().NotBe(metadata2);
    }

    [Fact]
    public void FileMetadata_WithCopy_CreatesNewInstance()
    {
        // Arrange
        FileMetadata original = new()
        {
            Size = 1024,
            Extension = "png",
            Dimensions = new FileDimensions { Width = 100, Height = 100 }
        };

        // Act
        FileMetadata copy = original with { Size = 2048 };

        // Assert
        _ = copy.Size.Should().Be(2048);
        _ = copy.Extension.Should().Be("png");
        _ = original.Size.Should().Be(1024);
    }

    [Theory]
    [InlineData("png")]
    [InlineData("jpg")]
    [InlineData("gif")]
    [InlineData("webp")]
    [InlineData("pdf")]
    [InlineData("mp4")]
    public void FileMetadata_AcceptsVariousExtensions(string extension)
    {
        // Arrange & Act
        FileMetadata metadata = new()
        {
            Extension = extension
        };

        // Assert
        _ = metadata.Extension.Should().Be(extension);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1024)]
    [InlineData(1048576)]
    [InlineData(long.MaxValue)]
    public void FileMetadata_AcceptsVariousSizes(long size)
    {
        // Arrange & Act
        FileMetadata metadata = new()
        {
            Size = size
        };

        // Assert
        _ = metadata.Size.Should().Be(size);
    }
}

/// <summary>
/// Unit tests for the <see cref="FileDimensions"/> record.
/// </summary>
public sealed class FileDimensionsTests
{
    [Fact]
    public void FileDimensions_DefaultValues_AreZero()
    {
        // Arrange & Act
        FileDimensions dimensions = new();

        // Assert
        _ = dimensions.Width.Should().Be(0);
        _ = dimensions.Height.Should().Be(0);
    }

    [Fact]
    public void FileDimensions_WithValues_SetsCorrectly()
    {
        // Arrange & Act
        FileDimensions dimensions = new()
        {
            Width = 1920,
            Height = 1080
        };

        // Assert
        _ = dimensions.Width.Should().Be(1920);
        _ = dimensions.Height.Should().Be(1080);
    }

    [Fact]
    public void FileDimensions_Equality_WorksCorrectly()
    {
        // Arrange
        FileDimensions dim1 = new()
        { Width = 100, Height = 100 };
        FileDimensions dim2 = new()
        { Width = 100, Height = 100 };

        // Assert
        _ = dim1.Should().Be(dim2);
    }

    [Fact]
    public void FileDimensions_Inequality_WhenWidthDiffers()
    {
        // Arrange
        FileDimensions dim1 = new()
        { Width = 100, Height = 100 };
        FileDimensions dim2 = new()
        { Width = 200, Height = 100 };

        // Assert
        _ = dim1.Should().NotBe(dim2);
    }

    [Fact]
    public void FileDimensions_Inequality_WhenHeightDiffers()
    {
        // Arrange
        FileDimensions dim1 = new()
        { Width = 100, Height = 100 };
        FileDimensions dim2 = new()
        { Width = 100, Height = 200 };

        // Assert
        _ = dim1.Should().NotBe(dim2);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(100, 100)]
    [InlineData(1920, 1080)]
    [InlineData(3840, 2160)]
    [InlineData(7680, 4320)]
    public void FileDimensions_AcceptsVariousSizes(int width, int height)
    {
        // Arrange & Act
        FileDimensions dimensions = new()
        {
            Width = width,
            Height = height
        };

        // Assert
        _ = dimensions.Width.Should().Be(width);
        _ = dimensions.Height.Should().Be(height);
    }
}
