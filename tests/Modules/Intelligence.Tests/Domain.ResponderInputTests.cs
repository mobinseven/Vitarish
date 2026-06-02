namespace Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="ResponderInput"/> record.
/// </summary>
public sealed class ResponderInputTests
{
    [Fact]
    public void TextInput_CreatesTextTypeInput()
    {
        // Arrange & Act
        ResponderInput input = ResponderInput.TextInput("Hello, world!");

        // Assert
        _ = input.Type.Should().Be(ResponderInputType.Text);
        _ = input.Text.Should().Be("Hello, world!");
        _ = input.ImageUrl.Should().BeNull();
        _ = input.Detail.Should().BeNull();
        _ = input.FileId.Should().BeNull();
        _ = input.FileData.Should().BeNull();
        _ = input.Filename.Should().BeNull();
        _ = input.ApprovalId.Should().BeNull();
    }

    [Fact]
    public void ImageInput_CreatesImageTypeInput()
    {
        // Arrange
        string imageUrl = "https://example.com/image.png";

        // Act
        ResponderInput input = ResponderInput.ImageInput(imageUrl);

        // Assert
        _ = input.Type.Should().Be(ResponderInputType.Image);
        _ = input.ImageUrl.Should().Be(imageUrl);
        _ = input.Text.Should().BeNull();
        _ = input.Detail.Should().BeNull();
    }

    [Fact]
    public void ImageInput_WithDetail_SetsDetailCorrectly()
    {
        // Arrange
        string imageUrl = "https://example.com/image.png";

        // Act
        ResponderInput input = ResponderInput.ImageInput(imageUrl, ResponderInputImageDetail.High);

        // Assert
        _ = input.Type.Should().Be(ResponderInputType.Image);
        _ = input.ImageUrl.Should().Be(imageUrl);
        _ = input.Detail.Should().Be(ResponderInputImageDetail.High);
    }

    [Theory]
    [InlineData(ResponderInputImageDetail.Low)]
    [InlineData(ResponderInputImageDetail.High)]
    [InlineData(ResponderInputImageDetail.Auto)]
    public void ImageInput_AcceptsAllImageDetailLevels(ResponderInputImageDetail detail)
    {
        // Arrange & Act
        ResponderInput input = ResponderInput.ImageInput("https://example.com/image.png", detail);

        // Assert
        _ = input.Detail.Should().Be(detail);
    }

    [Fact]
    public void FileInput_WithFileId_CreatesFileTypeInput()
    {
        // Arrange
        string fileId = "file-123";

        // Act
        ResponderInput input = ResponderInput.FileInput(fileId: fileId);

        // Assert
        _ = input.Type.Should().Be(ResponderInputType.File);
        _ = input.FileId.Should().Be(fileId);
        _ = input.FileData.Should().BeNull();
        _ = input.Filename.Should().BeNull();
    }

    [Fact]
    public void FileInput_WithFileDataAndFilename_CreatesFileTypeInput()
    {
        // Arrange
        string fileData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("test content"));
        string filename = "test.txt";

        // Act
        ResponderInput input = ResponderInput.FileInput(fileData: fileData, filename: filename);

        // Assert
        _ = input.Type.Should().Be(ResponderInputType.File);
        _ = input.FileId.Should().BeNull();
        _ = input.FileData.Should().Be(fileData);
        _ = input.Filename.Should().Be(filename);
    }

    [Fact]
    public void FileInput_WithAllParameters_SetsAllCorrectly()
    {
        // Arrange
        string fileId = "file-123";
        string fileData = "base64data";
        string filename = "document.pdf";

        // Act
        ResponderInput input = ResponderInput.FileInput(fileId: fileId, fileData: fileData, filename: filename);

        // Assert
        _ = input.Type.Should().Be(ResponderInputType.File);
        _ = input.FileId.Should().Be(fileId);
        _ = input.FileData.Should().Be(fileData);
        _ = input.Filename.Should().Be(filename);
    }

    [Fact]
    public void McpToolCallApprovalInput_CreatesMcpToolCallApprovalTypeInput()
    {
        // Arrange
        string approvalId = "approval-456";

        // Act
        ResponderInput input = ResponderInput.McpToolCallApprovalInput(approvalId);

        // Assert
        _ = input.Type.Should().Be(ResponderInputType.McpToolCallApproval);
        _ = input.ApprovalId.Should().Be(approvalId);
        _ = input.Text.Should().BeNull();
        _ = input.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void ResponderInput_Equality_WorksCorrectly()
    {
        // Arrange
        ResponderInput input1 = ResponderInput.TextInput("Hello");
        ResponderInput input2 = ResponderInput.TextInput("Hello");

        // Assert
        _ = input1.Should().Be(input2);
    }

    [Fact]
    public void ResponderInput_Inequality_WhenTypeDiffers()
    {
        // Arrange
        ResponderInput input1 = ResponderInput.TextInput("test");
        ResponderInput input2 = ResponderInput.ImageInput("test");

        // Assert
        _ = input1.Should().NotBe(input2);
    }

    [Fact]
    public void ResponderInput_Inequality_WhenTextDiffers()
    {
        // Arrange
        ResponderInput input1 = ResponderInput.TextInput("Hello");
        ResponderInput input2 = ResponderInput.TextInput("World");

        // Assert
        _ = input1.Should().NotBe(input2);
    }

    [Fact]
    public void ResponderInput_WithCopy_CreatesNewInstance()
    {
        // Arrange
        ResponderInput original = ResponderInput.TextInput("Hello");

        // Act
        ResponderInput copy = original with { Text = "Updated" };

        // Assert
        _ = copy.Text.Should().Be("Updated");
        _ = original.Text.Should().Be("Hello");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Short text")]
    [InlineData("A much longer text that contains multiple sentences. This could be a user's question or prompt.")]
    public void TextInput_AcceptsVariousTextLengths(string text)
    {
        // Arrange & Act
        ResponderInput input = ResponderInput.TextInput(text);

        // Assert
        _ = input.Text.Should().Be(text);
    }

    [Theory]
    [InlineData("https://example.com/image.png")]
    [InlineData("https://cdn.example.com/uploads/photo.jpg")]
    [InlineData("data:image/png;base64,iVBORw0KGgo=")]
    public void ImageInput_AcceptsVariousImageUrls(string imageUrl)
    {
        // Arrange & Act
        ResponderInput input = ResponderInput.ImageInput(imageUrl);

        // Assert
        _ = input.ImageUrl.Should().Be(imageUrl);
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
        _ = Enum.GetValues<ResponderInputType>().Should().Contain(ResponderInputType.Text);
        _ = Enum.GetValues<ResponderInputType>().Should().Contain(ResponderInputType.Image);
        _ = Enum.GetValues<ResponderInputType>().Should().Contain(ResponderInputType.File);
        _ = Enum.GetValues<ResponderInputType>().Should().Contain(ResponderInputType.McpToolCallApproval);
    }

    [Theory]
    [InlineData(ResponderInputType.Text, 0)]
    [InlineData(ResponderInputType.Image, 1)]
    [InlineData(ResponderInputType.File, 2)]
    [InlineData(ResponderInputType.McpToolCallApproval, 3)]
    public void ResponderInputType_HasCorrectIntValues(ResponderInputType type, int expectedValue) =>
        // Assert
        ((int)type).Should().Be(expectedValue);
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
        _ = Enum.GetValues<ResponderInputImageDetail>().Should().Contain(ResponderInputImageDetail.Low);
        _ = Enum.GetValues<ResponderInputImageDetail>().Should().Contain(ResponderInputImageDetail.High);
        _ = Enum.GetValues<ResponderInputImageDetail>().Should().Contain(ResponderInputImageDetail.Auto);
    }

    [Theory]
    [InlineData(ResponderInputImageDetail.Low, 0)]
    [InlineData(ResponderInputImageDetail.High, 1)]
    [InlineData(ResponderInputImageDetail.Auto, 2)]
    public void ResponderInputImageDetail_HasCorrectIntValues(ResponderInputImageDetail detail, int expectedValue) =>
        // Assert
        ((int)detail).Should().Be(expectedValue);
}
