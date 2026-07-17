using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Vitarish.Intelligence.Application.LiveTranscription;
using Vitarish.Intelligence.Contracts;

namespace Vitarish.Intelligence.Api;

internal sealed class RealtimeTranscriptionWebSocketSession(
    WebSocket socket,
    LiveTranscriptionSessionFactory sessionFactory,
    LiveTranscriptionSettings settings,
    ILogger<RealtimeTranscriptionWebSocketSession> logger)
{
    private const int MaxControlMessageBytes = 4 * 1024;
    private const int ReceiveBufferBytes = 16 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly RealtimeTranscriptionSocketWriter _writer = new(socket, JsonOptions);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        string pendingSessionId = Guid.NewGuid().ToString("N");
        LiveTranscriptionSession? session = null;

        try
        {
            IncomingMessage firstMessage;
            using (CancellationTokenSource startTimeout =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                startTimeout.CancelAfter(TimeSpan.FromSeconds(settings.IdleTimeoutSeconds));
                try
                {
                    firstMessage = await ReceiveMessageAsync(startTimeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    await SendFailureAndCloseAsync(
                        pendingSessionId,
                        new LiveTranscriptionError(
                            LiveTranscriptionErrorCode.IdleTimeout,
                            "The transcription session did not start before the connection timed out."),
                        CancellationToken.None).ConfigureAwait(false);
                    return;
                }
            }

            if (firstMessage.Type is WebSocketMessageType.Close)
            {
                await AcknowledgeClientCloseAsync(CancellationToken.None).ConfigureAwait(false);
                return;
            }

            if (!TryReadStart(firstMessage, out LiveTranscriptionRequest? request))
            {
                await SendFailureAndCloseAsync(
                    pendingSessionId,
                    new LiveTranscriptionError(
                        LiveTranscriptionErrorCode.InvalidStart,
                        "The first WebSocket message must be a valid transcription start message."),
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            string activeSessionId = pendingSessionId;
            LiveTranscriptionSessionCreationResult creation = sessionFactory.Create(
                request,
                (segment, token) => PublishSegmentAsync(activeSessionId, segment, token),
                cancellationToken);
            pendingSessionId = creation.SessionId;
            activeSessionId = creation.SessionId;
            if (!creation.Succeeded)
            {
                await SendFailureAndCloseAsync(
                    creation.SessionId,
                    creation.Error!,
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            session = creation.Session!;
            bool ready = await session.Ready.ConfigureAwait(false);
            if (!ready)
            {
                await SendOutcomeAndCloseAsync(
                    creation.SessionId,
                    await session.Completion.ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            await _writer.SendAsync(new RealtimeTranscriptionServerMessage(
                RealtimeTranscriptionMessageTypes.Started,
                creation.SessionId), cancellationToken).ConfigureAwait(false);

            await ProcessActiveSessionAsync(session, cancellationToken).ConfigureAwait(false);
        }
        catch (RealtimeTranscriptionProtocolException exception)
        {
            LiveTranscriptionError error = new(exception.Code, exception.ClientMessage);
            if (session is null)
            {
                await SendFailureAndCloseAsync(
                    pendingSessionId,
                    error.Code is LiveTranscriptionErrorCode.ProtocolViolation
                        ? error with { Code = LiveTranscriptionErrorCode.InvalidStart }
                        : error,
                    CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                session.Fail(error);
                await SendOutcomeAndCloseAsync(
                    session.SessionId,
                    await session.Completion.ConfigureAwait(false),
                    CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            session?.Cancel();
        }
        catch (Exception exception) when (
            exception is WebSocketException or IOException or InvalidOperationException)
        {
            session?.Cancel();
            logger.LogWarning(
                "Live transcription transport ended for session {SessionId} with transport failure type {FailureType}.",
                session?.SessionId ?? pendingSessionId,
                exception.GetType().Name);
        }
        catch (Exception exception)
        {
            LiveTranscriptionError error = new(
                LiveTranscriptionErrorCode.UnexpectedFailure,
                "The transcription session failed unexpectedly.");
            if (session is null)
            {
                await SendFailureAndCloseAsync(
                    pendingSessionId,
                    error,
                    CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                session.Fail(error);
                await SendOutcomeAndCloseAsync(
                    session.SessionId,
                    await session.Completion.ConfigureAwait(false),
                    CancellationToken.None).ConfigureAwait(false);
            }

            logger.LogError(
                "Live transcription session {SessionId} failed unexpectedly with failure type {FailureType}.",
                session?.SessionId ?? pendingSessionId,
                exception.GetType().Name);
        }
        finally
        {
            if (session is not null)
            {
                await session.DisposeAsync().ConfigureAwait(false);
            }

            await _writer.DisposeAsync().ConfigureAwait(false);
        }
    }

    private async Task ProcessActiveSessionAsync(
        LiveTranscriptionSession session,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            Task<IncomingMessage> receive = ReceiveMessageAsync(cancellationToken);
            Task first = await Task.WhenAny(receive, session.Completion).ConfigureAwait(false);
            if (first == session.Completion)
            {
                await SendOutcomeAndCloseAsync(
                    session.SessionId,
                    await session.Completion.ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
                socket.Abort();
                await ObserveReceiveAsync(receive).ConfigureAwait(false);
                return;
            }

            IncomingMessage message = await receive.ConfigureAwait(false);
            if (message.Type is WebSocketMessageType.Close)
            {
                session.Cancel();
                _ = await session.Completion.ConfigureAwait(false);
                await AcknowledgeClientCloseAsync(CancellationToken.None).ConfigureAwait(false);
                return;
            }

            if (message.Type is WebSocketMessageType.Binary)
            {
                LiveTranscriptionAcceptResult accepted = await session.AcceptAudioAsync(
                    message.Payload,
                    cancellationToken).ConfigureAwait(false);
                if (!accepted.Accepted)
                {
                    await SendOutcomeAndCloseAsync(
                        session.SessionId,
                        await session.Completion.ConfigureAwait(false),
                        cancellationToken).ConfigureAwait(false);
                    return;
                }

                continue;
            }

            if (!TryReadStop(message))
            {
                session.Fail(new LiveTranscriptionError(
                    LiveTranscriptionErrorCode.ProtocolViolation,
                    "Only binary audio or one stop message is valid after transcription starts."));
            }
            else
            {
                session.RequestStop();
            }

            await SendOutcomeAndCloseAsync(
                session.SessionId,
                await session.Completion.ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);
            return;
        }
    }

    private async ValueTask PublishSegmentAsync(
        string sessionId,
        LiveTranscriptionSegment segment,
        CancellationToken cancellationToken)
    {
        await _writer.SendAsync(new RealtimeTranscriptionServerMessage(
            RealtimeTranscriptionMessageTypes.Segment,
            sessionId,
            segment.Sequence,
            segment.Text), cancellationToken).ConfigureAwait(false);
    }

    private async Task<IncomingMessage> ReceiveMessageAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(ReceiveBufferBytes);
        try
        {
            using MemoryStream payload = new();
            WebSocketMessageType? messageType = null;
            while (true)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer, 0, ReceiveBufferBytes),
                    cancellationToken).ConfigureAwait(false);

                if (result.MessageType is WebSocketMessageType.Close)
                {
                    if (messageType is not null)
                    {
                        throw new RealtimeTranscriptionProtocolException(
                            LiveTranscriptionErrorCode.ProtocolViolation,
                            "The WebSocket message ended unexpectedly.");
                    }

                    return new IncomingMessage(WebSocketMessageType.Close, []);
                }

                messageType ??= result.MessageType;
                if (result.MessageType != messageType)
                {
                    throw new RealtimeTranscriptionProtocolException(
                        LiveTranscriptionErrorCode.ProtocolViolation,
                        "A WebSocket message cannot change its frame type.");
                }

                int limit = messageType is WebSocketMessageType.Text
                    ? MaxControlMessageBytes
                    : settings.MaxFrameBytes;
                if (payload.Length + result.Count > limit)
                {
                    throw new RealtimeTranscriptionProtocolException(
                        messageType is WebSocketMessageType.Binary
                            ? LiveTranscriptionErrorCode.FrameTooLarge
                            : LiveTranscriptionErrorCode.ProtocolViolation,
                        messageType is WebSocketMessageType.Binary
                            ? "The audio frame exceeds the configured limit."
                            : "The WebSocket control message exceeds the configured limit.");
                }

                payload.Write(buffer, 0, result.Count);
                if (result.EndOfMessage)
                {
                    return new IncomingMessage(messageType.Value, payload.ToArray());
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task SendOutcomeAndCloseAsync(
        string sessionId,
        LiveTranscriptionOutcome outcome,
        CancellationToken cancellationToken)
    {
        RealtimeTranscriptionServerMessage message = outcome.Kind switch
        {
            LiveTranscriptionOutcomeKind.Completed => new(
                RealtimeTranscriptionMessageTypes.Completed,
                sessionId),
            LiveTranscriptionOutcomeKind.Canceled => new(
                RealtimeTranscriptionMessageTypes.Canceled,
                sessionId,
                Error: ToContract(outcome.Error ?? new LiveTranscriptionError(
                    LiveTranscriptionErrorCode.Cancellation,
                    "The transcription session was canceled."))),
            _ => new(
                RealtimeTranscriptionMessageTypes.Failed,
                sessionId,
                Error: ToContract(outcome.Error ?? new LiveTranscriptionError(
                    LiveTranscriptionErrorCode.UnexpectedFailure,
                    "The transcription session failed unexpectedly.")))
        };

        bool sent = await _writer.SendTerminalAsync(message, cancellationToken).ConfigureAwait(false);
        if (outcome.Kind is LiveTranscriptionOutcomeKind.Failed)
        {
            logger.LogWarning(
                "Live transcription session {SessionId} failed with {ErrorCode}.",
                sessionId,
                outcome.Error?.Code ?? LiveTranscriptionErrorCode.UnexpectedFailure);
        }

        if (sent)
        {
            await CloseOutputAsync(ToCloseStatus(outcome), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendFailureAndCloseAsync(
        string sessionId,
        LiveTranscriptionError error,
        CancellationToken cancellationToken)
    {
        bool sent = await _writer.SendTerminalAsync(new RealtimeTranscriptionServerMessage(
            RealtimeTranscriptionMessageTypes.Failed,
            sessionId,
            Error: ToContract(error)), cancellationToken).ConfigureAwait(false);
        logger.LogWarning(
            "Live transcription connection {SessionId} was rejected with {ErrorCode}.",
            sessionId,
            error.Code);
        if (sent)
        {
            await CloseOutputAsync(ToCloseStatus(error.Code), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task CloseOutputAsync(
        WebSocketCloseStatus closeStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await socket.CloseOutputAsync(
                    closeStatus,
                    "Live transcription ended.",
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (
            exception is OperationCanceledException
                or WebSocketException
                or IOException
                or InvalidOperationException)
        {
            // The terminal message is authoritative; the close frame is best-effort cleanup.
        }
    }

    private async Task AcknowledgeClientCloseAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (socket.State is WebSocketState.CloseReceived)
            {
                await socket.CloseOutputAsync(
                    socket.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                    "Live transcription connection closed.",
                    cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (
            exception is OperationCanceledException
                or WebSocketException
                or IOException
                or InvalidOperationException)
        {
            // The peer has already closed; acknowledging the close is best-effort cleanup.
        }
    }

    private static bool TryReadStart(
        IncomingMessage message,
        [NotNullWhen(true)] out LiveTranscriptionRequest? request)
    {
        request = null;
        if (message.Type is not WebSocketMessageType.Text
            || !TryDeserializeControl(message.Payload, out RealtimeTranscriptionClientMessage? control)
            || control.Type != RealtimeTranscriptionMessageTypes.Start
            || string.IsNullOrWhiteSpace(control.Language)
            || string.IsNullOrWhiteSpace(control.MimeType))
        {
            return false;
        }

        request = new LiveTranscriptionRequest(control.Language, control.MimeType);
        return true;
    }

    private static bool TryReadStop(IncomingMessage message) =>
        message.Type is WebSocketMessageType.Text
        && TryDeserializeControl(message.Payload, out RealtimeTranscriptionClientMessage? control)
        && control.Type == RealtimeTranscriptionMessageTypes.Stop
        && control.Language is null
        && control.MimeType is null;

    private static bool TryDeserializeControl(
        byte[] payload,
        [NotNullWhen(true)] out RealtimeTranscriptionClientMessage? message)
    {
        message = null;
        try
        {
            message = JsonSerializer.Deserialize<RealtimeTranscriptionClientMessage>(
                StrictUtf8.GetString(payload),
                JsonOptions);
            return message is not null;
        }
        catch (Exception exception) when (
            exception is JsonException or DecoderFallbackException or NotSupportedException)
        {
            return false;
        }
    }

    private static RealtimeTranscriptionError ToContract(LiveTranscriptionError error) => new(
        error.Code switch
        {
            LiveTranscriptionErrorCode.InvalidStart => RealtimeTranscriptionErrorCode.InvalidStart,
            LiveTranscriptionErrorCode.ProtocolViolation => RealtimeTranscriptionErrorCode.ProtocolViolation,
            LiveTranscriptionErrorCode.UnsupportedLanguage => RealtimeTranscriptionErrorCode.UnsupportedLanguage,
            LiveTranscriptionErrorCode.UnsupportedMediaType => RealtimeTranscriptionErrorCode.UnsupportedMediaType,
            LiveTranscriptionErrorCode.FrameTooLarge => RealtimeTranscriptionErrorCode.FrameTooLarge,
            LiveTranscriptionErrorCode.PayloadTooLarge => RealtimeTranscriptionErrorCode.PayloadTooLarge,
            LiveTranscriptionErrorCode.DurationLimitExceeded => RealtimeTranscriptionErrorCode.DurationLimitExceeded,
            LiveTranscriptionErrorCode.IdleTimeout => RealtimeTranscriptionErrorCode.IdleTimeout,
            LiveTranscriptionErrorCode.ProviderUnavailable => RealtimeTranscriptionErrorCode.ProviderUnavailable,
            LiveTranscriptionErrorCode.ProviderTimeout => RealtimeTranscriptionErrorCode.ProviderTimeout,
            LiveTranscriptionErrorCode.InvalidAudio => RealtimeTranscriptionErrorCode.InvalidAudio,
            LiveTranscriptionErrorCode.Cancellation => RealtimeTranscriptionErrorCode.Cancellation,
            _ => RealtimeTranscriptionErrorCode.UnexpectedFailure
        },
        error.Message);

    private static WebSocketCloseStatus ToCloseStatus(LiveTranscriptionOutcome outcome) =>
        outcome.Kind is LiveTranscriptionOutcomeKind.Completed or LiveTranscriptionOutcomeKind.Canceled
            ? WebSocketCloseStatus.NormalClosure
            : ToCloseStatus(outcome.Error?.Code ?? LiveTranscriptionErrorCode.UnexpectedFailure);

    private static WebSocketCloseStatus ToCloseStatus(LiveTranscriptionErrorCode code) => code switch
    {
        LiveTranscriptionErrorCode.FrameTooLarge or LiveTranscriptionErrorCode.PayloadTooLarge =>
            WebSocketCloseStatus.MessageTooBig,
        LiveTranscriptionErrorCode.InvalidStart
            or LiveTranscriptionErrorCode.ProtocolViolation
            or LiveTranscriptionErrorCode.UnsupportedLanguage
            or LiveTranscriptionErrorCode.UnsupportedMediaType => WebSocketCloseStatus.PolicyViolation,
        _ => WebSocketCloseStatus.InternalServerError
    };

    private static async Task ObserveReceiveAsync(Task receive)
    {
        try
        {
            await receive.ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is OperationCanceledException
                or WebSocketException
                or IOException
                or InvalidOperationException
                or RealtimeTranscriptionProtocolException)
        {
            // The terminal response is authoritative; aborting the pending receive is cleanup only.
        }
    }

    private sealed record IncomingMessage(WebSocketMessageType Type, byte[] Payload);
}

internal sealed class RealtimeTranscriptionProtocolException(
    LiveTranscriptionErrorCode code,
    string clientMessage) : Exception(clientMessage)
{
    public LiveTranscriptionErrorCode Code { get; } = code;

    public string ClientMessage { get; } = clientMessage;
}

internal sealed class RealtimeTranscriptionSocketWriter(
    WebSocket socket,
    JsonSerializerOptions jsonOptions) : IAsyncDisposable
{
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private int _terminalSent;

    public async ValueTask SendAsync(
        RealtimeTranscriptionServerMessage message,
        CancellationToken cancellationToken)
    {
        await _sendGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (socket.State is not WebSocketState.Open)
            {
                throw new WebSocketException("The live transcription WebSocket is not writable.");
            }

            byte[] payload = JsonSerializer.SerializeToUtf8Bytes(message, jsonOptions);
            await socket.SendAsync(
                new ArraySegment<byte>(payload),
                WebSocketMessageType.Text,
                true,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendGate.Release();
        }
    }

    public async Task<bool> SendTerminalAsync(
        RealtimeTranscriptionServerMessage message,
        CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _terminalSent, 1) != 0)
        {
            return false;
        }

        try
        {
            await SendAsync(message, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception exception) when (
            exception is OperationCanceledException
                or WebSocketException
                or IOException
                or InvalidOperationException)
        {
            return false;
        }
    }

    public ValueTask DisposeAsync()
    {
        _sendGate.Dispose();
        return ValueTask.CompletedTask;
    }
}
