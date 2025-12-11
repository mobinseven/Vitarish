namespace Vitarish.Files.Tests;

/// <summary>
/// Unit tests for <see cref="FilesOptions"/>.
/// </summary>
public sealed class FilesOptionsTests
{
    [Fact]
    public void FilesOptions_DefaultValues_AreEmpty()
    {
        // Arrange & Act
        var options = new FilesOptions();

        // Assert
        options.Prefix.Should().BeEmpty();
        options.BaseUrl.Should().BeEmpty();
        options.UploadPath.Should().BeEmpty();
    }

    [Fact]
    public void FilesOptions_PropertiesAreSettable()
    {
        // Arrange
        var options = new FilesOptions();

        // Act
        options.Prefix = "files";
        options.BaseUrl = "https://files.example.com";
        options.UploadPath = "uploads";

        // Assert
        options.Prefix.Should().Be("files");
        options.BaseUrl.Should().Be("https://files.example.com");
        options.UploadPath.Should().Be("uploads");
    }

    [Theory]
    [InlineData("files")]
    [InlineData("uploads")]
    [InlineData("media")]
    [InlineData("content")]
    public void FilesOptions_AcceptsVariousPrefixes(string prefix)
    {
        // Arrange & Act
        var options = new FilesOptions
        {
            Prefix = prefix
        };

        // Assert
        options.Prefix.Should().Be(prefix);
    }

    [Theory]
    [InlineData("https://files.example.com")]
    [InlineData("https://cdn.example.com")]
    [InlineData("http://localhost:5000")]
    [InlineData("https://storage.example.com/v1")]
    public void FilesOptions_AcceptsVariousBaseUrls(string baseUrl)
    {
        // Arrange & Act
        var options = new FilesOptions
        {
            BaseUrl = baseUrl
        };

        // Assert
        options.BaseUrl.Should().Be(baseUrl);
    }

    [Theory]
    [InlineData("uploads")]
    [InlineData("files")]
    [InlineData("media/images")]
    [InlineData("content/documents")]
    public void FilesOptions_AcceptsVariousUploadPaths(string uploadPath)
    {
        // Arrange & Act
        var options = new FilesOptions
        {
            UploadPath = uploadPath
        };

        // Assert
        options.UploadPath.Should().Be(uploadPath);
    }
}
