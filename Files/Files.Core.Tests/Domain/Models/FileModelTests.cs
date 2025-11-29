namespace Vitarish.Files.Core.Tests.Domain.Models;

using Vitarish.Files.Core.Domain.Models;

public class FileMetadataTests
{
    [Fact]
    public void FileMetadata_NewInstance_HasNullProperties()
    {
        // Arrange & Act
        var metadata = new FileMetadata();

        // Assert
        Assert.Null(metadata.Extension);
        Assert.Null(metadata.Size);
        Assert.Null(metadata.OriginalFileName);
        Assert.Null(metadata.CustomMetadata);
    }

    [Fact]
    public void FileMetadata_SetProperties_ValuesAreCorrect()
    {
        // Arrange & Act
        var metadata = new FileMetadata
        {
            Extension = ".pdf",
            Size = 2048,
            OriginalFileName = "document.pdf",
            CustomMetadata = new Dictionary<string, string> { ["key"] = "value" }
        };

        // Assert
        Assert.Equal(".pdf", metadata.Extension);
        Assert.Equal(2048, metadata.Size);
        Assert.Equal("document.pdf", metadata.OriginalFileName);
        Assert.Single(metadata.CustomMetadata!);
        Assert.Equal("value", metadata.CustomMetadata["key"]);
    }
}

public class OwnerStatisticsTests
{
    [Fact]
    public void OwnerStatistics_Constructor_SetsProperties()
    {
        // Arrange & Act
        var stats = new OwnerStatistics("owner-123", 10, 10240);

        // Assert
        Assert.Equal("owner-123", stats.Owner);
        Assert.Equal(10, stats.Count);
        Assert.Equal(10240, stats.TotalSize);
    }
}

public class FileInformationDescriptorTests
{
    [Fact]
    public void FileInformationDescriptor_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var descriptor = new FileInformationDescriptor();

        // Assert
        Assert.Equal(Guid.Empty, descriptor.Id);
        Assert.Null(descriptor.Url);
        Assert.Null(descriptor.Name);
        Assert.Null(descriptor.Owner);
        Assert.Null(descriptor.Checksum);
        Assert.Null(descriptor.Tag);
    }

    [Fact]
    public void FileInformationDescriptor_SetProperties_ValuesAreCorrect()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var descriptor = new FileInformationDescriptor
        {
            Id = id,
            Url = "https://example.com/file.pdf",
            Name = "file.pdf",
            Owner = "owner-1",
            Checksum = "hash123",
            Tag = "document"
        };

        // Assert
        Assert.Equal(id, descriptor.Id);
        Assert.Equal("https://example.com/file.pdf", descriptor.Url);
        Assert.Equal("file.pdf", descriptor.Name);
        Assert.Equal("owner-1", descriptor.Owner);
        Assert.Equal("hash123", descriptor.Checksum);
        Assert.Equal("document", descriptor.Tag);
    }
}

public class FileInformationDescriptorDtoTests
{
    [Fact]
    public void FileInformationDescriptorDto_NewInstance_HasDefaultValues()
    {
        // Arrange & Act
        var descriptor = new FileInformationDescriptorDto();

        // Assert
        Assert.Equal(Guid.Empty, descriptor.Id);
        Assert.Null(descriptor.Url);
        Assert.Null(descriptor.Name);
        Assert.Null(descriptor.Owner);
        Assert.Null(descriptor.Checksum);
        Assert.Null(descriptor.Tag);
    }

    [Fact]
    public void FileInformationDescriptorDto_SetProperties_ValuesAreCorrect()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var descriptor = new FileInformationDescriptorDto
        {
            Id = id,
            Url = "https://example.com/image.png",
            Name = "image.png",
            Owner = "user-1",
            Checksum = "xyz789",
            Tag = "image"
        };

        // Assert
        Assert.Equal(id, descriptor.Id);
        Assert.Equal("https://example.com/image.png", descriptor.Url);
        Assert.Equal("image.png", descriptor.Name);
        Assert.Equal("user-1", descriptor.Owner);
        Assert.Equal("xyz789", descriptor.Checksum);
        Assert.Equal("image", descriptor.Tag);
    }
}
