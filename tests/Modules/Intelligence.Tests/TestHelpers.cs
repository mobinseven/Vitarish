using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Vitarish.Intelligence.Application.Abstractions;

namespace Vitarish.Intelligence.Tests;

/// <summary>
/// Helper extensions for testing async enumerables.
/// </summary>
public static class AsyncEnumerableTestExtensions
{
    /// <summary>
    /// Creates an async enumerable that throws the specified exception when enumerated.
    /// </summary>
    public static async IAsyncEnumerable<T> ThrowOnEnumerateAsync<T>(Exception exception)
    {
        await Task.Yield();
        throw exception;
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }
}

public sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    where T : class, new()
{
    public TestOptionsMonitor(T currentValue)
    {
        CurrentValue = currentValue;
    }

    public T CurrentValue { get; }

    public T Get(string? name) => CurrentValue;

    public IDisposable OnChange(Action<T, string?> listener) => Disposable.Empty;

    private sealed class Disposable : IDisposable
    {
        public static readonly IDisposable Empty = new Disposable();

        public void Dispose()
        {
        }
    }
}

/// <summary>
/// A mock implementation of IResponder for testing purposes.
/// </summary>
public sealed class MockResponder : IResponder
{
    private readonly IReadOnlyList<ResponderOutput>? _generateResult;
    private readonly IAsyncEnumerable<ResponderOutput>? _streamResult;
    private readonly Exception? _exception;

    public List<(IReadOnlyList<ResponderInput> Input, ResponderOptions Options)> GenerateCalls { get; } = [];
    public List<(IReadOnlyList<ResponderInput> Input, ResponderOptions Options)> StreamCalls { get; } = [];

    public MockResponder(IReadOnlyList<ResponderOutput>? generateResult = null, IAsyncEnumerable<ResponderOutput>? streamResult = null, Exception? exception = null)
    {
        _generateResult = generateResult;
        _streamResult = streamResult;
        _exception = exception;
    }

    public static MockResponder WithTextResponse(string text) => new(
            generateResult: [ResponderOutput.TextOutput(text)],
            streamResult: new[] { ResponderOutput.TextOutput(text) }.ToAsyncEnumerable());

    public static MockResponder WithException(Exception exception) => new(exception: exception);

    public Task<IReadOnlyList<ResponderOutput>> GenerateAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, CancellationToken ct = default)
    {
        GenerateCalls.Add((input, options));

        return _exception is not null
            ? throw _exception
            : Task.FromResult(_generateResult ?? Array.Empty<ResponderOutput>());
    }

    public async IAsyncEnumerable<ResponderOutput> StreamAsync(IReadOnlyList<ResponderInput> input, ResponderOptions options, [EnumeratorCancellation] CancellationToken ct = default)
    {
        StreamCalls.Add((input, options));

        if (_exception is not null)
        {
            throw _exception;
        }

        if (_streamResult is not null)
        {
            await foreach (ResponderOutput? output in _streamResult.WithCancellation(ct))
            {
                yield return output;
            }
        }
    }
}

/// <summary>
/// A mock implementation of ITranscriber for testing purposes.
/// </summary>
public sealed class MockTranscriber : ITranscriber
{
    private readonly string? _transcribeResult;
    private readonly IAsyncEnumerable<string>? _streamResult;
    private readonly Exception? _exception;

    public List<(Stream Audio, string MimeType, string? Language, string? ModelId)> TranscribeCalls { get; } = [];
    public List<(string MimeType, string? Language, string? ModelId)> StreamCalls { get; } = [];

    public MockTranscriber(string? transcribeResult = null, IAsyncEnumerable<string>? streamResult = null, Exception? exception = null)
    {
        _transcribeResult = transcribeResult;
        _streamResult = streamResult;
        _exception = exception;
    }

    public static MockTranscriber WithResult(string result) => new(
            transcribeResult: result,
            streamResult: new[] { result }.ToAsyncEnumerable());

    public static MockTranscriber WithException(Exception exception) => new(exception: exception);

    public Task<string?> TranscribeAsync(Stream audio, string mimeType, string? language, string? modelId, CancellationToken cancellationToken)
    {
        TranscribeCalls.Add((audio, mimeType, language, modelId));

        return _exception is not null ? throw _exception : Task.FromResult(_transcribeResult);
    }

    public async IAsyncEnumerable<string> StreamTranscribeAsync(ChannelReader<byte[]> audio, string mimeType, string? language, string? modelId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        StreamCalls.Add((mimeType, language, modelId));

        if (_exception is not null)
        {
            throw _exception;
        }

        if (_streamResult is not null)
        {
            await foreach (var chunk in _streamResult.WithCancellation(cancellationToken))
            {
                yield return chunk;
            }
        }
    }
}

/// <summary>
/// A mock implementation of ITranscriptionProvider for testing purposes.
/// </summary>
public sealed class MockTranscriptionProvider : ITranscriptionProvider
{
    private readonly HashSet<string> _supportedModels;
    private readonly string? _transcribeResult;
    private readonly IAsyncEnumerable<string>? _streamResult;
    private readonly Exception? _exception;

    public List<(Stream Audio, string MimeType, string? Language, string? ModelId)> TranscribeCalls { get; } = [];
    public List<string?> SupportsModelCalls { get; } = [];

    public MockTranscriptionProvider(
        IEnumerable<string>? supportedModels = null,
        string? transcribeResult = null,
        IAsyncEnumerable<string>? streamResult = null,
        Exception? exception = null)
    {
        _supportedModels = supportedModels?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        _transcribeResult = transcribeResult;
        _streamResult = streamResult;
        _exception = exception;
    }

    public static MockTranscriptionProvider WithSupportedModels(params string[] models) => new(supportedModels: models);

    public bool SupportsModel(string? modelId)
    {
        SupportsModelCalls.Add(modelId);
        return modelId is not null && _supportedModels.Contains(modelId);
    }

    public Task<string?> TranscribeAsync(Stream audio, string mimeType, string? language, string? modelId, CancellationToken cancellationToken)
    {
        TranscribeCalls.Add((audio, mimeType, language, modelId));

        return _exception is not null ? throw _exception : Task.FromResult(_transcribeResult);
    }

    public async IAsyncEnumerable<string> StreamTranscribeAsync(ChannelReader<byte[]> audio, string mimeType, string? language, string? modelId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        if (_streamResult is not null)
        {
            await foreach (var chunk in _streamResult.WithCancellation(cancellationToken))
            {
                yield return chunk;
            }
        }
    }
}

/// <summary>
/// Test helper for creating AiOptions with various configurations.
/// </summary>
public static class AiOptionsFactory
{
    public static IOptionsMonitor<AiOptions> CreateWithOpenAi(string apiKey = "test-api-key") => Options.Create(new AiOptions()
    {
        DefaultResponseModel = "gpt-5-nano-2025-08-07",
        DefaultTranscriptionModel = "gpt-4o-mini-transcribe",
        OpenAi = new OpenAiOptions { ApiKey = apiKey }
    }).ToMonitor();

    public static IOptionsMonitor<AiOptions> CreateWithOllama(string baseUrl = "http://localhost:11434") => Options.Create(new AiOptions()
    {
        DefaultResponseModel = "qwen3:4b",
        DefaultTranscriptionModel = "whisper",
        Ollama = new OllamaOptions { BaseUrl = baseUrl }
    }).ToMonitor();

    public static IOptionsMonitor<AiOptions> CreateWithGoogle(string geminiApiKey = "gemini-key", string speechProjectId = "project-id") => Options.Create(new AiOptions()
    {
        DefaultResponseModel = "gemini-2.5-pro",
        DefaultTranscriptionModel = "chirp",
        Google = new GoogleOptions
        {
            Gemini = new GoogleGeminiOptions { ApiKey = geminiApiKey },
            Speech = new GoogleSpeechOptions
            {
                ProjectId = speechProjectId,
                Location = "us-central1",
                RecognizerId = "default"
            }
            }
    }).ToMonitor();

    public static IOptionsMonitor<AiOptions> CreateWithAllProviders() => Options.Create(new AiOptions()
    {
        DefaultResponseModel = "gpt-5-nano-2025-08-07",
        DefaultTranscriptionModel = "gpt-4o-mini-transcribe",
        OpenAi = new OpenAiOptions { ApiKey = "test-api-key" },
        Ollama = new OllamaOptions { BaseUrl = "http://localhost:11434" },
        Google = new GoogleOptions
        {
            Gemini = new GoogleGeminiOptions { ApiKey = "gemini-key" },
            Speech = new GoogleSpeechOptions
            {
                ProjectId = "project-id",
                Location = "us-central1",
                RecognizerId = "default"
            }
            }
    }).ToMonitor();

    public static IOptionsMonitor<AiOptions> CreateWithNoProviders() => Options.Create(new AiOptions()
    {
        OpenAi = new OpenAiOptions { ApiKey = "" },
        Ollama = new OllamaOptions { BaseUrl = "" },
        Google = new GoogleOptions
        {
            Gemini = new GoogleGeminiOptions { ApiKey = "" },
            Speech = new GoogleSpeechOptions { ProjectId = "" }
        }
    }).ToMonitor();
}

public static class OptionsMonitorExtensions
{
    public static IOptionsMonitor<T> ToMonitor<T>(this IOptions<T> options)
        where T : class, new() => new TestOptionsMonitor<T>(options.Value);

    public static IOptionsMonitor<T> ToMonitor<T>(this IOptionsMonitor<T> options)
        where T : class, new() => options;
}

/// <summary>
/// Test helper for generating audio test data.
/// </summary>
public static class AudioTestDataGenerator
{
    /// <summary>
    /// Generates a minimal valid WAV file header with silent audio data.
    /// </summary>
    public static byte[] GenerateDummyWavData(int dataSize = 32)
    {
        const int sampleRate = 16000;
        const short bitsPerSample = 16;
        const short channels = 1;

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // RIFF header
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);

        // fmt subchunk
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8);
        writer.Write((short)(channels * bitsPerSample / 8));
        writer.Write(bitsPerSample);

        // data subchunk
        writer.Write("data"u8);
        writer.Write(dataSize);
        writer.Write(new byte[dataSize]);

        return ms.ToArray();
    }

    /// <summary>
    /// Creates a memory stream containing dummy WAV audio data.
    /// </summary>
    public static MemoryStream CreateDummyAudioStream(int dataSize = 32) => new(GenerateDummyWavData(dataSize));
}
