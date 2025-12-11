using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Infrastructure.Services;
using Vitarish.Intelligence.Infrastructure.Services.Dispatching;

namespace Vitarish.Intelligence.Tests;

/// <summary>
/// Unit tests for the <see cref="IntelligenceStartupSelfTest"/> class.
/// </summary>
public sealed class IntelligenceStartupSelfTestTests
{
    private const string DefaultResponseModel = "gpt-5-nano-2025-08-07";
    private const string DefaultTranscriptionModel = "gpt-4o-mini-transcribe";

    [Fact]
    public async Task StartAsync_WithValidConfiguration_CompletesSuccessfully()
    {
        // Arrange
        var aiOptions = CreateAiOptionsWithOpenAi();
        var modelRegistry = new ModelRegistry(aiOptions);
        var responder = new Mock<IResponder>();
        var transcriber = new Mock<ITranscriber>();
        var logger = NullLogger<IntelligenceStartupSelfTest>.Instance;

        SetupResponderSuccess(responder);
        SetupTranscriberSuccess(transcriber);

        var sut = new IntelligenceStartupSelfTest(aiOptions, modelRegistry, responder.Object, transcriber.Object, logger);

        // Act
        var act = () => sut.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        responder.Verify(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        transcriber.Verify(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_WithUnmappedResponseModel_ThrowsInvalidOperationException()
    {
        // Arrange
        var aiOptions = CreateAiOptionsWithOpenAi();
        aiOptions.CurrentValue.DefaultResponseModel = "unknown-model";
        var modelRegistry = new ModelRegistry(aiOptions);
        var responder = new Mock<IResponder>();
        var transcriber = new Mock<ITranscriber>();
        var logger = NullLogger<IntelligenceStartupSelfTest>.Instance;

        var sut = new IntelligenceStartupSelfTest(aiOptions, modelRegistry, responder.Object, transcriber.Object, logger);

        // Act
        var act = () => sut.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unknown-model*not mapped*");
    }

    [Fact]
    public async Task StartAsync_WithUnmappedTranscriptionModel_ThrowsInvalidOperationException()
    {
        // Arrange
        var aiOptions = CreateAiOptionsWithOpenAi();
        aiOptions.CurrentValue.DefaultTranscriptionModel = "unknown-transcription-model";
        var modelRegistry = new ModelRegistry(aiOptions);
        var responder = new Mock<IResponder>();
        var transcriber = new Mock<ITranscriber>();
        var logger = NullLogger<IntelligenceStartupSelfTest>.Instance;

        var sut = new IntelligenceStartupSelfTest(aiOptions, modelRegistry, responder.Object, transcriber.Object, logger);

        // Act
        var act = () => sut.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unknown-transcription-model*not mapped*");
    }

    [Fact]
    public async Task StartAsync_WhenResponderFails_ThrowsException()
    {
        // Arrange
        var aiOptions = CreateAiOptionsWithOpenAi();
        var modelRegistry = new ModelRegistry(aiOptions);
        var responder = new Mock<IResponder>();
        var transcriber = new Mock<ITranscriber>();
        var logger = NullLogger<IntelligenceStartupSelfTest>.Instance;

        responder
            .Setup(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API unreachable"));

        var sut = new IntelligenceStartupSelfTest(aiOptions, modelRegistry, responder.Object, transcriber.Object, logger);

        // Act
        var act = () => sut.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("API unreachable");
    }

    [Fact]
    public async Task StartAsync_WhenResponderReturnsEmptyResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var aiOptions = CreateAiOptionsWithOpenAi();
        var modelRegistry = new ModelRegistry(aiOptions);
        var responder = new Mock<IResponder>();
        var transcriber = new Mock<ITranscriber>();
        var logger = NullLogger<IntelligenceStartupSelfTest>.Instance;

        responder
            .Setup(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ResponderOutput>());

        var sut = new IntelligenceStartupSelfTest(aiOptions, modelRegistry, responder.Object, transcriber.Object, logger);

        // Act
        var act = () => sut.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*empty response*");
    }

    [Fact]
    public async Task StartAsync_WhenTranscriberFails_ThrowsException()
    {
        // Arrange
        var aiOptions = CreateAiOptionsWithOpenAi();
        var modelRegistry = new ModelRegistry(aiOptions);
        var responder = new Mock<IResponder>();
        var transcriber = new Mock<ITranscriber>();
        var logger = NullLogger<IntelligenceStartupSelfTest>.Instance;

        SetupResponderSuccess(responder);
        transcriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Transcription API unreachable"));

        var sut = new IntelligenceStartupSelfTest(aiOptions, modelRegistry, responder.Object, transcriber.Object, logger);

        // Act
        var act = () => sut.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Transcription API unreachable");
    }

    [Fact]
    public async Task StartAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var aiOptions = CreateAiOptionsWithOpenAi();
        var modelRegistry = new ModelRegistry(aiOptions);
        var responder = new Mock<IResponder>();
        var transcriber = new Mock<ITranscriber>();
        var logger = NullLogger<IntelligenceStartupSelfTest>.Instance;

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        responder
            .Setup(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var sut = new IntelligenceStartupSelfTest(aiOptions, modelRegistry, responder.Object, transcriber.Object, logger);

        // Act
        var act = () => sut.StartAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StopAsync_Always_CompletesImmediately()
    {
        // Arrange
        var aiOptions = CreateAiOptionsWithOpenAi();
        var modelRegistry = new ModelRegistry(aiOptions);
        var responder = new Mock<IResponder>();
        var transcriber = new Mock<ITranscriber>();
        var logger = NullLogger<IntelligenceStartupSelfTest>.Instance;

        var sut = new IntelligenceStartupSelfTest(aiOptions, modelRegistry, responder.Object, transcriber.Object, logger);

        // Act
        var act = () => sut.StopAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_WithModelNotInAvailableModels_LogsWarningButContinues()
    {
        // Arrange - configure without OpenAI API key, so models won't be in available list
        var aiOptions = Options.Create(new AiOptions
        {
            DefaultResponseModel = DefaultResponseModel,
            DefaultTranscriptionModel = DefaultTranscriptionModel,
            // No OpenAI API key, so models won't be "available"
            OpenAi = new OpenAiOptions { ApiKey = "" }
        }).ToMonitor();
        var modelRegistry = new ModelRegistry(aiOptions);
        var responder = new Mock<IResponder>();
        var transcriber = new Mock<ITranscriber>();
        var mockLogger = new Mock<ILogger<IntelligenceStartupSelfTest>>();

        SetupResponderSuccess(responder);
        SetupTranscriberSuccess(transcriber);

        var sut = new IntelligenceStartupSelfTest(aiOptions, modelRegistry, responder.Object, transcriber.Object, mockLogger.Object);

        // Act
        var act = () => sut.StartAsync(CancellationToken.None);

        // Assert - should still complete, but logs warnings
        await act.Should().NotThrowAsync();

        // Verify warnings were logged for unavailable models
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("may not be available")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(1));
    }

    [Fact]
    public async Task StartAsync_UsesCorrectModelInResponderOptions()
    {
        // Arrange
        var aiOptions = CreateAiOptionsWithOpenAi();
        var modelRegistry = new ModelRegistry(aiOptions);
        var responder = new Mock<IResponder>();
        var transcriber = new Mock<ITranscriber>();
        var logger = NullLogger<IntelligenceStartupSelfTest>.Instance;

        ResponderOptions? capturedOptions = null;
        responder
            .Setup(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<ResponderInput>, ResponderOptions, CancellationToken>((_, options, _) => capturedOptions = options)
            .ReturnsAsync([ResponderOutput.TextOutput("ok")]);

        SetupTranscriberSuccess(transcriber);

        var sut = new IntelligenceStartupSelfTest(aiOptions, modelRegistry, responder.Object, transcriber.Object, logger);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Model.Should().Be(DefaultResponseModel);
    }

    [Fact]
    public async Task StartAsync_UsesCorrectModelInTranscriberCall()
    {
        // Arrange
        var aiOptions = CreateAiOptionsWithOpenAi();
        var modelRegistry = new ModelRegistry(aiOptions);
        var responder = new Mock<IResponder>();
        var transcriber = new Mock<ITranscriber>();
        var logger = NullLogger<IntelligenceStartupSelfTest>.Instance;

        string? capturedModelId = null;
        SetupResponderSuccess(responder);
        transcriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, string, string?, string?, CancellationToken>((_, _, _, modelId, _) => capturedModelId = modelId)
            .ReturnsAsync("test transcription");

        var sut = new IntelligenceStartupSelfTest(aiOptions, modelRegistry, responder.Object, transcriber.Object, logger);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        capturedModelId.Should().Be(DefaultTranscriptionModel);
    }

    [Fact]
    public async Task StartAsync_UsesWebmMimeTypeForTranscriber()
    {
        // Arrange
        var aiOptions = CreateAiOptionsWithOpenAi();
        var modelRegistry = new ModelRegistry(aiOptions);
        var responder = new Mock<IResponder>();
        var transcriber = new Mock<ITranscriber>();
        var logger = NullLogger<IntelligenceStartupSelfTest>.Instance;

        string? capturedMimeType = null;
        SetupResponderSuccess(responder);
        transcriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, string, string?, string?, CancellationToken>((_, mimeType, _, _, _) => capturedMimeType = mimeType)
            .ReturnsAsync("test transcription");

        var sut = new IntelligenceStartupSelfTest(aiOptions, modelRegistry, responder.Object, transcriber.Object, logger);

        // Act
        await sut.StartAsync(CancellationToken.None);

        // Assert
        capturedMimeType.Should().Be("audio/webm");
    }

    private static IOptionsMonitor<AiOptions> CreateAiOptionsWithOpenAi() => Options.Create(new AiOptions
    {
        DefaultResponseModel = DefaultResponseModel,
        DefaultTranscriptionModel = DefaultTranscriptionModel,
        OpenAi = new OpenAiOptions { ApiKey = "test-api-key" }
    }).ToMonitor();

    private static void SetupResponderSuccess(Mock<IResponder> responder)
    {
        responder
            .Setup(r => r.GenerateAsync(It.IsAny<IReadOnlyList<ResponderInput>>(), It.IsAny<ResponderOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([ResponderOutput.TextOutput("ok")]);
    }

    private static void SetupTranscriberSuccess(Mock<ITranscriber> transcriber)
    {
        transcriber
            .Setup(t => t.TranscribeAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test transcription");
    }
}
