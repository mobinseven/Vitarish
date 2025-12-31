using Microsoft.JSInterop;
using System.Threading.Channels;
using Vitarish.Intelligence.Application.Abstractions;

namespace Vitarish.Intelligence.Interface.Services;

public sealed class StreamTranscriber(ITranscriber speechHubClient, IJSRuntime jsRuntime) : IStreamTranscriber
{
    private const string ModulePath = "./_content/Vitarish.Intelligence.Interface/Components/AiStreamTranscriber.razor.js";
    private const int ChannelCapacity = 5000;

    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private IJSObjectReference? _module;
    private DotNetObjectReference<StreamTranscriber>? _objRef;
    private Channel<byte[]>? _audioRecorder;
    private bool _initialized;
    private bool _disposed;

    public bool Supported { get; private set; }

    public bool Recording { get; private set; }

    public bool Transcribing { get; private set; }

    public string Value { get; private set; } = string.Empty;

    public TranscriberOptions? Options { get; set; }

    public event EventHandler<bool>? SupportStatusChanged;

    public event EventHandler<string>? ValueChanged;

    public event EventHandler<string>? ChunkReceived;

    public event EventHandler<bool>? RecordingStateChanged;

    public event EventHandler<bool>? TranscribingStateChanged;

    public event EventHandler? TranscriptionCompleted;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
        {
            return;
        }

        await _initializationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath).ConfigureAwait(false);
            _objRef ??= DotNetObjectReference.Create(this);
            Supported = await _module.InvokeAsync<bool>("supports").ConfigureAwait(false);
            _initialized = true;
            SupportStatusChanged?.Invoke(this, Supported);
        }
        finally
        {
            _ = _initializationGate.Release();
        }
    }

    public async Task ToggleRecordingAsync(CancellationToken cancellationToken = default)
    {
        if (Recording)
        {
            await StopRecordingAsync().ConfigureAwait(false);
        }
        else
        {
            await StartRecordingAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task StartRecordingAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken).ConfigureAwait(false);

        if (!Supported || Recording || Transcribing)
        {
            return;
        }

        if (Value.Length > 0)
        {
            Value = string.Empty;
            ValueChanged?.Invoke(this, Value);
        }

        Recording = true;
        RecordingStateChanged?.Invoke(this, true);
        _audioRecorder = Channel.CreateBounded<byte[]>(ChannelCapacity);

        await _module!.InvokeVoidAsync("start", _objRef).ConfigureAwait(false);

        StringBuilder builder = new();

        try
        {
            await foreach (string chunk in speechHubClient.StreamTranscribeAsync(_audioRecorder.Reader, modelId: Options?.ModelId, cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                if (string.IsNullOrEmpty(chunk))
                {
                    continue;
                }

                ChunkReceived?.Invoke(this, chunk);
                _ = builder.Append(chunk);
                string nextValue = builder.ToString();

                if (!string.Equals(Value, nextValue, StringComparison.Ordinal))
                {
                    Value = nextValue;
                    ValueChanged?.Invoke(this, Value);
                }
            }
        }
        finally
        {
            _audioRecorder = null;
            Transcribing = false;
            TranscribingStateChanged?.Invoke(this, false);
            TranscriptionCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    public async ValueTask StopRecordingAsync()
    {
        if (!Recording)
        {
            return;
        }

        Transcribing = true;
        TranscribingStateChanged?.Invoke(this, true);

        ValueTask stopTask = _module?.InvokeVoidAsync("stop") ?? ValueTask.CompletedTask;

        _ = (_audioRecorder?.Writer.TryComplete());
        Recording = false;
        RecordingStateChanged?.Invoke(this, false);

        await stopTask.ConfigureAwait(false);
    }

    [JSInvokable]
    public ValueTask OnChunk(byte[] data)
    {
        _ = (_audioRecorder?.Writer.TryWrite(data));
        return ValueTask.CompletedTask;
    }

    [JSInvokable]
    public ValueTask OnStop()
    {
        if (Recording)
        {
            Recording = false;
            RecordingStateChanged?.Invoke(this, false);
        }

        _ = (_audioRecorder?.Writer.TryComplete());
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            await StopRecordingAsync().ConfigureAwait(false);

            if (_module is not null)
            {
                await _module.DisposeAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _objRef?.Dispose();
            _objRef = null;
            _module = null;
            _audioRecorder = null;
        }
    }
}
