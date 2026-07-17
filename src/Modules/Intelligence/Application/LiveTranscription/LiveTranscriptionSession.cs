using System.Threading.Channels;
using Vitarish.Intelligence.Application.Abstractions;

namespace Vitarish.Intelligence.Application.LiveTranscription;

public sealed class LiveTranscriptionSession : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly ITranscriptionProvider _provider;
    private readonly LiveTranscriptionSettings _settings;
    private readonly TimeProvider _timeProvider;
    private readonly LiveTranscriptionProviderRequest _request;
    private readonly Func<LiveTranscriptionSegment, CancellationToken, ValueTask> _publishSegment;
    private readonly Channel<byte[]> _audio;
    private readonly CancellationToken _externalCancellationToken;
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private readonly CancellationTokenSource _monitorCancellation = new();
    private readonly CancellationTokenRegistration _externalCancellationRegistration;
    private readonly TaskCompletionSource<bool> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Task<LiveTranscriptionOutcome> _completion;
    private LiveTranscriptionSessionState _state = LiveTranscriptionSessionState.Connecting;
    private LiveTranscriptionError? _failure;
    private DateTimeOffset _lastAudioAt;
    private long _totalBytes;
    private long _nextSequence;
    private int _disposeStarted;

    internal LiveTranscriptionSession(
        string sessionId,
        ITranscriptionProvider provider,
        LiveTranscriptionSettings settings,
        TimeProvider timeProvider,
        LiveTranscriptionProviderRequest request,
        Func<LiveTranscriptionSegment, CancellationToken, ValueTask> publishSegment,
        CancellationToken externalCancellationToken)
    {
        SessionId = sessionId;
        _provider = provider;
        _settings = settings;
        _timeProvider = timeProvider;
        _request = request;
        _publishSegment = publishSegment;
        _externalCancellationToken = externalCancellationToken;
        _audio = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(settings.ChannelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });
        _externalCancellationRegistration = externalCancellationToken.Register(Cancel);
        _completion = RunAsync();
    }

    public string SessionId { get; }

    public Task<bool> Ready => _ready.Task;

    public Task<LiveTranscriptionOutcome> Completion => _completion;

    public LiveTranscriptionSessionState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    public async ValueTask<LiveTranscriptionAcceptResult> AcceptAudioAsync(
        ReadOnlyMemory<byte> audio,
        CancellationToken cancellationToken)
    {
        if (audio.IsEmpty)
        {
            return LiveTranscriptionAcceptResult.Success;
        }

        LiveTranscriptionError? rejection = null;
        lock (_gate)
        {
            if (_state is not LiveTranscriptionSessionState.Active)
            {
                rejection = new(LiveTranscriptionErrorCode.ProtocolViolation,
                    "Audio is accepted only while the transcription session is active.");
            }
            else if (audio.Length > _settings.MaxFrameBytes)
            {
                rejection = new(LiveTranscriptionErrorCode.FrameTooLarge,
                    "The audio frame exceeds the configured limit.");
            }
            else if (_totalBytes + audio.Length > _settings.MaxTotalBytes)
            {
                rejection = new(LiveTranscriptionErrorCode.PayloadTooLarge,
                    "The transcription session exceeds the configured audio limit.");
            }
            else
            {
                _totalBytes += audio.Length;
                _lastAudioAt = _timeProvider.GetUtcNow();
            }
        }

        if (rejection is not null)
        {
            Fail(rejection);
            return LiveTranscriptionAcceptResult.Rejected(rejection);
        }

        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _lifetimeCancellation.Token);
        try
        {
            await _audio.Writer.WriteAsync(audio.ToArray(), linked.Token).ConfigureAwait(false);
            return LiveTranscriptionAcceptResult.Success;
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
            LiveTranscriptionError error = CurrentFailureOrCancellation();
            return LiveTranscriptionAcceptResult.Rejected(error);
        }
    }

    public void RequestStop()
    {
        lock (_gate)
        {
            if (_state is not LiveTranscriptionSessionState.Active)
            {
                return;
            }

            _state = LiveTranscriptionSessionState.Completing;
        }

        _audio.Writer.TryComplete();
    }

    public void Fail(LiveTranscriptionError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        lock (_gate)
        {
            if (IsTerminal(_state))
            {
                return;
            }

            _failure ??= error;
            _state = LiveTranscriptionSessionState.Failed;
        }

        _lifetimeCancellation.Cancel();
        _audio.Writer.TryComplete();
    }

    public void Cancel()
    {
        lock (_gate)
        {
            if (IsTerminal(_state))
            {
                return;
            }

            _state = LiveTranscriptionSessionState.Canceled;
        }

        _lifetimeCancellation.Cancel();
        _audio.Writer.TryComplete();
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0)
        {
            return;
        }

        Cancel();
        try
        {
            _ = await _completion.ConfigureAwait(false);
        }
        catch
        {
            // Completion maps all expected provider/session failures to a stable outcome.
        }

        _externalCancellationRegistration.Dispose();
        _monitorCancellation.Dispose();
        _lifetimeCancellation.Dispose();
    }

    private async Task<LiveTranscriptionOutcome> RunAsync()
    {
        Task durationMonitor = MonitorDurationAsync();
        Task idleMonitor = MonitorIdleAsync();
        Task providerMonitor = MonitorProviderOperationAsync();

        try
        {
            if (_lifetimeCancellation.IsCancellationRequested)
            {
                lock (_gate)
                {
                    _state = LiveTranscriptionSessionState.Canceled;
                    return CanceledOutcome();
                }
            }

            await _provider.TranscribeAsync(
                _audio.Reader,
                _request,
                HandleProviderEventAsync,
                _lifetimeCancellation.Token).ConfigureAwait(false);

            lock (_gate)
            {
                if (_failure is not null)
                {
                    _state = LiveTranscriptionSessionState.Failed;
                    return new(LiveTranscriptionOutcomeKind.Failed, _failure);
                }

                if (_state is LiveTranscriptionSessionState.Canceled)
                {
                    return CanceledOutcome();
                }

                if (_state is not LiveTranscriptionSessionState.Completing)
                {
                    _state = LiveTranscriptionSessionState.Failed;
                    _failure = UnexpectedProviderCompletion();
                    return new(LiveTranscriptionOutcomeKind.Failed, _failure);
                }

                _state = LiveTranscriptionSessionState.Completed;
                return new(LiveTranscriptionOutcomeKind.Completed);
            }
        }
        catch (TranscriptionProviderException exception)
        {
            return CompleteFailure(new(exception.Code, exception.Message));
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
            lock (_gate)
            {
                if (_failure is not null)
                {
                    _state = LiveTranscriptionSessionState.Failed;
                    return new(LiveTranscriptionOutcomeKind.Failed, _failure);
                }

                _state = LiveTranscriptionSessionState.Canceled;
                return CanceledOutcome();
            }
        }
        catch (Exception)
        {
            return CompleteFailure(new(
                LiveTranscriptionErrorCode.UnexpectedFailure,
                "The transcription session failed unexpectedly."));
        }
        finally
        {
            _audio.Writer.TryComplete();
            _monitorCancellation.Cancel();
            _ready.TrySetResult(false);
            await ObserveMonitorCompletionAsync(durationMonitor, idleMonitor, providerMonitor)
                .ConfigureAwait(false);
        }
    }

    private async ValueTask HandleProviderEventAsync(
        TranscriptionProviderEvent providerEvent,
        CancellationToken cancellationToken)
    {
        if (providerEvent.Kind is TranscriptionProviderEventKind.Ready)
        {
            bool becameReady = false;
            lock (_gate)
            {
                if (_state is LiveTranscriptionSessionState.Connecting)
                {
                    _state = LiveTranscriptionSessionState.Active;
                    _lastAudioAt = _timeProvider.GetUtcNow();
                    becameReady = true;
                }
            }

            if (!becameReady)
            {
                throw new TranscriptionProviderException(
                    LiveTranscriptionErrorCode.ProtocolViolation,
                    "The transcription provider reported an invalid ready transition.");
            }

            _ready.TrySetResult(true);
            return;
        }

        string text = providerEvent.Text?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            return;
        }

        long sequence;
        lock (_gate)
        {
            if (_state is not (LiveTranscriptionSessionState.Active or LiveTranscriptionSessionState.Completing))
            {
                throw new TranscriptionProviderException(
                    LiveTranscriptionErrorCode.ProtocolViolation,
                    "The transcription provider emitted a segment outside an active session.");
            }

            sequence = _nextSequence++;
        }

        await _publishSegment(new LiveTranscriptionSegment(sequence, text), cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task MonitorDurationAsync()
    {
        if (!await WaitUntilReadyAsync().ConfigureAwait(false))
        {
            return;
        }

        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(_settings.MaxDurationSeconds),
                _timeProvider,
                _monitorCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_monitorCancellation.IsCancellationRequested)
        {
            return;
        }

        lock (_gate)
        {
            if (_state is not LiveTranscriptionSessionState.Active)
            {
                return;
            }
        }

        Fail(new(
            LiveTranscriptionErrorCode.DurationLimitExceeded,
            "The transcription session reached its maximum duration."));
    }

    private async Task MonitorIdleAsync()
    {
        if (!await WaitUntilReadyAsync().ConfigureAwait(false))
        {
            return;
        }

        while (!_monitorCancellation.IsCancellationRequested)
        {
            TimeSpan remaining;
            lock (_gate)
            {
                if (_state is not LiveTranscriptionSessionState.Active)
                {
                    return;
                }

                DateTimeOffset expiresAt = _lastAudioAt.AddSeconds(_settings.IdleTimeoutSeconds);
                remaining = expiresAt - _timeProvider.GetUtcNow();
            }

            if (remaining <= TimeSpan.Zero)
            {
                Fail(new(
                    LiveTranscriptionErrorCode.IdleTimeout,
                    "The transcription session ended because no audio frames were received."));
                return;
            }

            try
            {
                await Task.Delay(remaining, _timeProvider, _monitorCancellation.Token)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (_monitorCancellation.IsCancellationRequested)
            {
                return;
            }
        }
    }

    private async Task MonitorProviderOperationAsync()
    {
        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(_settings.ProviderOperationTimeoutSeconds),
                _timeProvider,
                _monitorCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_monitorCancellation.IsCancellationRequested)
        {
            return;
        }

        Fail(new(
            LiveTranscriptionErrorCode.ProviderTimeout,
            "The transcription provider operation timed out."));
    }

    private async Task<bool> WaitUntilReadyAsync()
    {
        try
        {
            return await _ready.Task.WaitAsync(_monitorCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_monitorCancellation.IsCancellationRequested)
        {
            return false;
        }
    }

    private LiveTranscriptionOutcome CompleteFailure(LiveTranscriptionError error)
    {
        lock (_gate)
        {
            _failure ??= error;
            _state = LiveTranscriptionSessionState.Failed;
            return new(LiveTranscriptionOutcomeKind.Failed, _failure);
        }
    }

    private LiveTranscriptionError CurrentFailureOrCancellation()
    {
        lock (_gate)
        {
            return _failure ?? new(
                LiveTranscriptionErrorCode.Cancellation,
                "The transcription session was canceled.");
        }
    }

    private static LiveTranscriptionOutcome CanceledOutcome() => new(
        LiveTranscriptionOutcomeKind.Canceled,
        new LiveTranscriptionError(
            LiveTranscriptionErrorCode.Cancellation,
            "The transcription session was canceled."));

    private static LiveTranscriptionError UnexpectedProviderCompletion() => new(
        LiveTranscriptionErrorCode.ProviderUnavailable,
        "The transcription provider ended before the client stopped the session.");

    private static bool IsTerminal(LiveTranscriptionSessionState state) =>
        state is LiveTranscriptionSessionState.Completed
            or LiveTranscriptionSessionState.Failed
            or LiveTranscriptionSessionState.Canceled;

    private static async Task ObserveMonitorCompletionAsync(params Task[] monitors)
    {
        try
        {
            await Task.WhenAll(monitors).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Session completion intentionally cancels limit monitors.
        }
    }
}
