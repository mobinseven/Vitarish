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
        var metadata = new FileMetadata();

        // Assert
        metadata.Size.Should().Be(0);
        metadata.Extension.Should().BeNull();
        metadata.Dimensions.Should().BeNull();
    }

    [Fact]
    public void FileMetadata_WithSize_SetsCorrectly()
    {
        // Arrange & Act
        var metadata = new FileMetadata
        {
            Size = 1024
        };

        // Assert
        metadata.Size.Should().Be(1024);
    }

    [Fact]
    public void FileMetadata_WithExtension_SetsCorrectly()
    {
        // Arrange & Act
        var metadata = new FileMetadata
        {
            Extension = "png"
        };

        // Assert
        metadata.Extension.Should().Be("png");
    }

    [Fact]
    public void FileMetadata_WithDimensions_SetsCorrectly()
    {
        // Arrange
        var dimensions = new FileDimensions
        {
            Width = 1920,
            Height = 1080
        };

        // Act
        var metadata = new FileMetadata
        {
            Dimensions = dimensions
        };

        // Assert
        metadata.Dimensions.Should().NotBeNull();
        metadata.Dimensions!.Width.Should().Be(1920);
        metadata.Dimensions.Height.Should().Be(1080);
    }

    [Fact]
    public void FileMetadata_WithAllProperties_SetsCorrectly()
    {
        // Arrange
        var dimensions = new FileDimensions
        {
            Width = 800,
            Height = 600
        };

        // Act
        var metadata = new FileMetadata
        {
            Size = 2048,
            Extension = "jpg",
            Dimensions = dimensions
        };

        // Assert
        metadata.Size.Should().Be(2048);
        metadata.Extension.Should().Be("jpg");
        metadata.Dimensions.Should().NotBeNull();
        metadata.Dimensions!.Width.Should().Be(800);
        metadata.Dimensions.Height.Should().Be(600);
    }

    [Fact]
    public void FileMetadata_Equality_WorksCorrectly()
    {
        // Arrange
        var metadata1 = new FileMetadata
        {
            Size = 1024,
            Extension = "png"
        };
        var metadata2 = new FileMetadata
        {
            Size = 1024,
            Extension = "png"
        };

        // Assert
        metadata1.Should().Be(metadata2);
    }

    [Fact]
    public void FileMetadata_Equality_WithDimensions_WorksCorrectly()
    {
        // Arrange
        var metadata1 = new FileMetadata
        {
            Size = 1024,
            Extension = "png",
            Dimensions = new FileDimensions { Width = 100, Height = 100 }
        };
        var metadata2 = new FileMetadata
        {
            Size = 1024,
            Extension = "png",
            Dimensions = new FileDimensions { Width = 100, Height = 100 }
        };

        // Assert
        metadata1.Should().Be(metadata2);
    }

    [Fact]
    public void FileMetadata_Inequality_WhenSizeDiffers()
    {
        // Arrange
        var metadata1 = new FileMetadata { Size = 1024 };
        var metadata2 = new FileMetadata { Size = 2048 };

        // Assert
        metadata1.Should().NotBe(metadata2);
    }

    [Fact]
    public void FileMetadata_Inequality_WhenExtensionDiffers()
    {
        // Arrange
        var metadata1 = new FileMetadata { Extension = "png" };
        var metadata2 = new FileMetadata { Extension = "jpg" };

        // Assert
        metadata1.Should().NotBe(metadata2);
    }

    [Fact]
    public void FileMetadata_Inequality_WhenDimensionsDiffer()
    {
        // Arrange
        var metadata1 = new FileMetadata
        {
            Dimensions = new FileDimensions { Width = 100, Height = 100 }
        };
        var metadata2 = new FileMetadata
        {
            Dimensions = new FileDimensions { Width = 200, Height = 200 }
        };

        // Assert
        metadata1.Should().NotBe(metadata2);
    }

    [Fact]
    public void FileMetadata_WithCopy_CreatesNewInstance()
    {
        // Arrange
        var original = new FileMetadata
        {
            Size = 1024,
            Extension = "png",
            Dimensions = new FileDimensions { Width = 100, Height = 100 }
        };

        // Act
        FileMetadata copy = original with { Size = 2048 };

        // Assert
        copy.Size.Should().Be(2048);
        copy.Extension.Should().Be("png");
        original.Size.Should().Be(1024);
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
        var metadata = new FileMetadata
        {
            Extension = extension
        };

        // Assert
        metadata.Extension.Should().Be(extension);
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
        var metadata = new FileMetadata
        {
            Size = size
        };

        // Assert
        metadata.Size.Should().Be(size);
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
        var dimensions = new FileDimensions();

        // Assert
        dimensions.Width.Should().Be(0);
        dimensions.Height.Should().Be(0);
    }

    [Fact]
    public void FileDimensions_WithValues_SetsCorrectly()
    {
        // Arrange & Act
        var dimensions = new FileDimensions
        {
            Width = 1920,
            Height = 1080
        };

        // Assert
        dimensions.Width.Should().Be(1920);
        dimensions.Height.Should().Be(1080);
    }

    [Fact]
    public void FileDimensions_Equality_WorksCorrectly()
    {
        // Arrange
        var dim1 = new FileDimensions { Width = 100, Height = 100 };
        var dim2 = new FileDimensions { Width = 100, Height = 100 };

        // Assert
        dim1.Should().Be(dim2);
    }

    [Fact]
    public void FileDimensions_Inequality_WhenWidthDiffers()
    {
        // Arrange
        var dim1 = new FileDimensions { Width = 100, Height = 100 };
        var dim2 = new FileDimensions { Width = 200, Height = 100 };

        // Assert
        dim1.Should().NotBe(dim2);
    }

    [Fact]
    public void FileDimensions_Inequality_WhenHeightDiffers()
    {
        // Arrange
        var dim1 = new FileDimensions { Width = 100, Height = 100 };
        var dim2 = new FileDimensions { Width = 100, Height = 200 };

        // Assert
        dim1.Should().NotBe(dim2);
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
        var dimensions = new FileDimensions
        {
            Width = width,
            Height = height
        };

        // Assert
        dimensions.Width.Should().Be(width);
        dimensions.Height.Should().Be(height);
    }
}
