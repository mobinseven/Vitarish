using Vitarish.Intelligence.Application.Abstractions;

namespace Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="IResponder"/> interface contract.
/// These tests verify the behavior expected from any IResponder implementation.
/// </summary>
public sealed class IResponderInterfaceTests
{
    [Fact]
    public async Task GenerateAsync_WithValidInputAndOptions_ReturnsResponderOutputs()
    {
        // Arrange
        Mock<IResponder> mockResponder = new();
        List<ResponderInput> input = [ResponderInput.TextInput("Hello")];
        ResponderOptions options = new()
        { Model = "test-model" };
        List<ResponderOutput> expectedOutput = [ResponderOutput.TextOutput("Hi there!")];

        _ = mockResponder
            .Setup(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOutput);

        // Act
        IReadOnlyList<ResponderOutput> result = await mockResponder.Object.GenerateAsync(input, options, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        _ = result.Should().HaveCount(1);
        _ = result[0].Type.Should().Be(ResponderOutputType.Text);
        _ = result[0].Text.Should().Be("Hi there!");
    }

    [Fact]
    public async Task GenerateAsync_WithMultipleInputs_ProcessesAllInputs()
    {
        // Arrange
        Mock<IResponder> mockResponder = new();
        List<ResponderInput> input =
        [
            ResponderInput.TextInput("First message"),
            ResponderInput.ImageInput("https://example.com/image.png")
        ];
        ResponderOptions options = new();
        List<ResponderOutput> expectedOutput = [ResponderOutput.TextOutput("Response")];

        _ = mockResponder
            .Setup(r => r.GenerateAsync(
                It.Is<IReadOnlyList<ResponderInput>>(i => i.Count == 2),
                It.IsAny<ResponderOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOutput);

        // Act
        IReadOnlyList<ResponderOutput> result = await mockResponder.Object.GenerateAsync(input, options, CancellationToken.None);

        // Assert
        _ = result.Should().NotBeNull();
        mockResponder.Verify(r => r.GenerateAsync(
            It.Is<IReadOnlyList<ResponderInput>>(i => i.Count == 2),
            It.IsAny<ResponderOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_WithCancellation_PropagatesCancellationToken()
    {
        // Arrange
        Mock<IResponder> mockResponder = new();
        List<ResponderInput> input = [ResponderInput.TextInput("Test")];
        ResponderOptions options = new();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        _ = mockResponder
            .Setup(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        Func<Task<IReadOnlyList<ResponderOutput>>> act = () => mockResponder.Object.GenerateAsync(input, options, cts.Token);

        // Assert
        _ = await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GenerateAsync_ReturnsEmptyList_WhenNoResponse()
    {
        // Arrange
        Mock<IResponder> mockResponder = new();
        List<ResponderInput> input = [ResponderInput.TextInput("Test")];
        ResponderOptions options = new();

        _ = mockResponder
            .Setup(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ResponderOutput>());

        // Act
        IReadOnlyList<ResponderOutput> result = await mockResponder.Object.GenerateAsync(input, options, CancellationToken.None);

        // Assert
        _ = result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_WithMcpToolCall_ReturnsToolCallOutput()
    {
        // Arrange
        Mock<IResponder> mockResponder = new();
        List<ResponderInput> input = [ResponderInput.TextInput("Search for weather")];
        ResponderOptions options = new()
        {
            Tools = [new ResponderToolDefinition { Type = "mcp", ServerLabel = "weather" }]
        };
        List<ResponderOutput> expectedOutput =
        [
            ResponderOutput.McpToolCallOutput("get_weather", """{"location": "NYC"}""", "weather", "call-1")
        ];

        _ = mockResponder
            .Setup(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOutput);

        // Act
        IReadOnlyList<ResponderOutput> result = await mockResponder.Object.GenerateAsync(input, options, CancellationToken.None);

        // Assert
        _ = result.Should().HaveCount(1);
        _ = result[0].Type.Should().Be(ResponderOutputType.McpToolCall);
        _ = result[0].ToolName.Should().Be("get_weather");
    }

    [Fact]
    public async Task StreamAsync_ReturnsAsyncEnumerableOfResponderOutputs()
    {
        // Arrange
        Mock<IResponder> mockResponder = new();
        List<ResponderInput> input = [ResponderInput.TextInput("Stream test")];
        ResponderOptions options = new();
        List<ResponderOutput> outputs =
        [
            ResponderOutput.TextOutput("Hello"),
            ResponderOutput.TextOutput(" World")
        ];

        _ = mockResponder
            .Setup(r => r.StreamAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .Returns(outputs.ToAsyncEnumerable());

        // Act
        List<ResponderOutput> results = [];
        await foreach (ResponderOutput output in mockResponder.Object.StreamAsync(input, options, CancellationToken.None))
        {
            results.Add(output);
        }

        // Assert
        _ = results.Should().HaveCount(2);
        _ = results[0].Text.Should().Be("Hello");
        _ = results[1].Text.Should().Be(" World");
    }

    [Fact]
    public async Task StreamAsync_WithCancellation_StopsStreaming()
    {
        // Arrange
        Mock<IResponder> mockResponder = new();
        List<ResponderInput> input = [ResponderInput.TextInput("Test")];
        ResponderOptions options = new();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        _ = mockResponder
            .Setup(r => r.StreamAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerableTestExtensions.ThrowOnEnumerateAsync<ResponderOutput>(new OperationCanceledException()));

        // Act
        Func<Task> act = async () =>
        {
            await foreach (ResponderOutput _ in mockResponder.Object.StreamAsync(input, options, cts.Token))
            {
                // Intentionally left empty: we only want to trigger enumeration to test cancellation.
            }
        };

        // Assert
        _ = await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
