namespace Vitarish.Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="ResponderInput"/> record.
/// </summary>
public sealed class ResponderInputTests
{
    [Fact]
    public void TextInput_CreatesTextTypeInput()
    {
        // Arrange & Act
        var input = ResponderInput.TextInput("Hello, world!");

        // Assert
        input.Type.Should().Be(ResponderInputType.Text);
        input.Text.Should().Be("Hello, world!");
        input.ImageUrl.Should().BeNull();
        input.Detail.Should().BeNull();
        input.FileId.Should().BeNull();
        input.FileData.Should().BeNull();
        input.Filename.Should().BeNull();
        input.ApprovalId.Should().BeNull();
    }

    [Fact]
    public void ImageInput_CreatesImageTypeInput()
    {
        // Arrange
        var imageUrl = "https://example.com/image.png";

        // Act
        var input = ResponderInput.ImageInput(imageUrl);

        // Assert
        input.Type.Should().Be(ResponderInputType.Image);
        input.ImageUrl.Should().Be(imageUrl);
        input.Text.Should().BeNull();
        input.Detail.Should().BeNull();
    }

    [Fact]
    public void ImageInput_WithDetail_SetsDetailCorrectly()
    {
        // Arrange
        var imageUrl = "https://example.com/image.png";

        // Act
        var input = ResponderInput.ImageInput(imageUrl, ResponderInputImageDetail.High);

        // Assert
        input.Type.Should().Be(ResponderInputType.Image);
        input.ImageUrl.Should().Be(imageUrl);
        input.Detail.Should().Be(ResponderInputImageDetail.High);
    }

    [Theory]
    [InlineData(ResponderInputImageDetail.Low)]
    [InlineData(ResponderInputImageDetail.High)]
    [InlineData(ResponderInputImageDetail.Auto)]
    public void ImageInput_AcceptsAllImageDetailLevels(ResponderInputImageDetail detail)
    {
        // Arrange & Act
        var input = ResponderInput.ImageInput("https://example.com/image.png", detail);

        // Assert
        input.Detail.Should().Be(detail);
    }

    [Fact]
    public void FileInput_WithFileId_CreatesFileTypeInput()
    {
        // Arrange
        var fileId = "file-123";

        // Act
        var input = ResponderInput.FileInput(fileId: fileId);

        // Assert
        input.Type.Should().Be(ResponderInputType.File);
        input.FileId.Should().Be(fileId);
        input.FileData.Should().BeNull();
        input.Filename.Should().BeNull();
    }

    [Fact]
    public void FileInput_WithFileDataAndFilename_CreatesFileTypeInput()
    {
        // Arrange
        var fileData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test content"));
        var filename = "test.txt";

        // Act
        var input = ResponderInput.FileInput(fileData: fileData, filename: filename);

        // Assert
        input.Type.Should().Be(ResponderInputType.File);
        input.FileId.Should().BeNull();
        input.FileData.Should().Be(fileData);
        input.Filename.Should().Be(filename);
    }

    [Fact]
    public void FileInput_WithAllParameters_SetsAllCorrectly()
    {
        // Arrange
        var fileId = "file-123";
        var fileData = "base64data";
        var filename = "document.pdf";

        // Act
        var input = ResponderInput.FileInput(fileId: fileId, fileData: fileData, filename: filename);

        // Assert
        input.Type.Should().Be(ResponderInputType.File);
        input.FileId.Should().Be(fileId);
        input.FileData.Should().Be(fileData);
        input.Filename.Should().Be(filename);
    }

    [Fact]
    public void McpToolCallApprovalInput_CreatesMcpToolCallApprovalTypeInput()
    {
        // Arrange
        var approvalId = "approval-456";

        // Act
        var input = ResponderInput.McpToolCallApprovalInput(approvalId);

        // Assert
        input.Type.Should().Be(ResponderInputType.McpToolCallApproval);
        input.ApprovalId.Should().Be(approvalId);
        input.Text.Should().BeNull();
        input.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void ResponderInput_Equality_WorksCorrectly()
    {
        // Arrange
        var input1 = ResponderInput.TextInput("Hello");
        var input2 = ResponderInput.TextInput("Hello");

        // Assert
        input1.Should().Be(input2);
    }

    [Fact]
    public void ResponderInput_Inequality_WhenTypeDiffers()
    {
        // Arrange
        var input1 = ResponderInput.TextInput("test");
        var input2 = ResponderInput.ImageInput("test");

        // Assert
        input1.Should().NotBe(input2);
    }

    [Fact]
    public void ResponderInput_Inequality_WhenTextDiffers()
    {
        // Arrange
        var input1 = ResponderInput.TextInput("Hello");
        var input2 = ResponderInput.TextInput("World");

        // Assert
        input1.Should().NotBe(input2);
    }

    [Fact]
    public void ResponderInput_WithCopy_CreatesNewInstance()
    {
        // Arrange
        var original = ResponderInput.TextInput("Hello");

        // Act
        var copy = original with { Text = "Updated" };

        // Assert
        copy.Text.Should().Be("Updated");
        original.Text.Should().Be("Hello");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Short text")]
    [InlineData("A much longer text that contains multiple sentences. This could be a user's question or prompt.")]
    public void TextInput_AcceptsVariousTextLengths(string text)
    {
        // Arrange & Act
        var input = ResponderInput.TextInput(text);

        // Assert
        input.Text.Should().Be(text);
    }

    [Theory]
    [InlineData("https://example.com/image.png")]
    [InlineData("https://cdn.example.com/uploads/photo.jpg")]
    [InlineData("data:image/png;base64,iVBORw0KGgo=")]
    public void ImageInput_AcceptsVariousImageUrls(string imageUrl)
    {
        // Arrange & Act
        var input = ResponderInput.ImageInput(imageUrl);

        // Assert
        input.ImageUrl.Should().Be(imageUrl);
    }
}

/// <summary>
/// Unit tests for the <see cref="ResponderInputType"/> enum.
/// </summary>
public sealed class ResponderInputTypeTests
{
    [Fact]
    public void ResponderInputType_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<ResponderInputType>().Should().Contain(ResponderInputType.Text);
        Enum.GetValues<ResponderInputType>().Should().Contain(ResponderInputType.Image);
        Enum.GetValues<ResponderInputType>().Should().Contain(ResponderInputType.File);
        Enum.GetValues<ResponderInputType>().Should().Contain(ResponderInputType.McpToolCallApproval);
    }

    [Theory]
    [InlineData(ResponderInputType.Text, 0)]
    [InlineData(ResponderInputType.Image, 1)]
    [InlineData(ResponderInputType.File, 2)]
    [InlineData(ResponderInputType.McpToolCallApproval, 3)]
    public void ResponderInputType_HasCorrectIntValues(ResponderInputType type, int expectedValue)
    {
        // Assert
        ((int)type).Should().Be(expectedValue);
    }
}

/// <summary>
/// Unit tests for the <see cref="ResponderInputImageDetail"/> enum.
/// </summary>
public sealed class ResponderInputImageDetailTests
{
    [Fact]
    public void ResponderInputImageDetail_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<ResponderInputImageDetail>().Should().Contain(ResponderInputImageDetail.Low);
        Enum.GetValues<ResponderInputImageDetail>().Should().Contain(ResponderInputImageDetail.High);
        Enum.GetValues<ResponderInputImageDetail>().Should().Contain(ResponderInputImageDetail.Auto);
    }

    [Theory]
    [InlineData(ResponderInputImageDetail.Low, 0)]
    [InlineData(ResponderInputImageDetail.High, 1)]
    [InlineData(ResponderInputImageDetail.Auto, 2)]
    public void ResponderInputImageDetail_HasCorrectIntValues(ResponderInputImageDetail detail, int expectedValue)
    {
        // Assert
        ((int)detail).Should().Be(expectedValue);
    }
}
