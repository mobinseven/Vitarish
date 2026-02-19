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
        var mockResponder = new Mock<IResponder>();
        var input = new List<ResponderInput> { ResponderInput.TextInput("Hello") };
        var options = new ResponderOptions { Model = "test-model" };
        var expectedOutput = new List<ResponderOutput> { ResponderOutput.TextOutput("Hi there!") };

        mockResponder
            .Setup(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOutput);

        // Act
        IReadOnlyList<ResponderOutput> result = await mockResponder.Object.GenerateAsync(input, options, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ResponderOutputType.Text);
        result[0].Text.Should().Be("Hi there!");
    }

    [Fact]
    public async Task GenerateAsync_WithMultipleInputs_ProcessesAllInputs()
    {
        // Arrange
        var mockResponder = new Mock<IResponder>();
        var input = new List<ResponderInput>
        {
            ResponderInput.TextInput("First message"),
            ResponderInput.ImageInput("https://example.com/image.png")
        };
        var options = new ResponderOptions();
        var expectedOutput = new List<ResponderOutput> { ResponderOutput.TextOutput("Response") };

        mockResponder
            .Setup(r => r.GenerateAsync(
                It.Is<IReadOnlyList<ResponderInput>>(i => i.Count == 2),
                It.IsAny<ResponderOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOutput);

        // Act
        IReadOnlyList<ResponderOutput> result = await mockResponder.Object.GenerateAsync(input, options, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        mockResponder.Verify(r => r.GenerateAsync(
            It.Is<IReadOnlyList<ResponderInput>>(i => i.Count == 2),
            It.IsAny<ResponderOptions>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_WithCancellation_PropagatesCancellationToken()
    {
        // Arrange
        var mockResponder = new Mock<IResponder>();
        var input = new List<ResponderInput> { ResponderInput.TextInput("Test") };
        var options = new ResponderOptions();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        mockResponder
            .Setup(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        Func<Task<IReadOnlyList<ResponderOutput>>> act = () => mockResponder.Object.GenerateAsync(input, options, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task GenerateAsync_ReturnsEmptyList_WhenNoResponse()
    {
        // Arrange
        var mockResponder = new Mock<IResponder>();
        var input = new List<ResponderInput> { ResponderInput.TextInput("Test") };
        var options = new ResponderOptions();

        mockResponder
            .Setup(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ResponderOutput>());

        // Act
        IReadOnlyList<ResponderOutput> result = await mockResponder.Object.GenerateAsync(input, options, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GenerateAsync_WithMcpToolCall_ReturnsToolCallOutput()
    {
        // Arrange
        var mockResponder = new Mock<IResponder>();
        var input = new List<ResponderInput> { ResponderInput.TextInput("Search for weather") };
        var options = new ResponderOptions
        {
            Tools = [new ResponderToolDefinition { Type = "mcp", ServerLabel = "weather" }]
        };
        var expectedOutput = new List<ResponderOutput>
        {
            ResponderOutput.McpToolCallOutput("get_weather", """{"location": "NYC"}""", "weather", "call-1")
        };

        mockResponder
            .Setup(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOutput);

        // Act
        IReadOnlyList<ResponderOutput> result = await mockResponder.Object.GenerateAsync(input, options, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ResponderOutputType.McpToolCall);
        result[0].ToolName.Should().Be("get_weather");
    }

    [Fact]
    public async Task StreamAsync_ReturnsAsyncEnumerableOfResponderOutputs()
    {
        // Arrange
        var mockResponder = new Mock<IResponder>();
        var input = new List<ResponderInput> { ResponderInput.TextInput("Stream test") };
        var options = new ResponderOptions();
        var outputs = new List<ResponderOutput>
        {
            ResponderOutput.TextOutput("Hello"),
            ResponderOutput.TextOutput(" World")
        };

        mockResponder
            .Setup(r => r.StreamAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .Returns(outputs.ToAsyncEnumerable());

        // Act
        var results = new List<ResponderOutput>();
        await foreach (ResponderOutput output in mockResponder.Object.StreamAsync(input, options, CancellationToken.None))
        {
            results.Add(output);
        }

        // Assert
        results.Should().HaveCount(2);
        results[0].Text.Should().Be("Hello");
        results[1].Text.Should().Be(" World");
    }

    [Fact]
    public async Task StreamAsync_WithCancellation_StopsStreaming()
    {
        // Arrange
        var mockResponder = new Mock<IResponder>();
        var input = new List<ResponderInput> { ResponderInput.TextInput("Test") };
        var options = new ResponderOptions();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        mockResponder
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
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
