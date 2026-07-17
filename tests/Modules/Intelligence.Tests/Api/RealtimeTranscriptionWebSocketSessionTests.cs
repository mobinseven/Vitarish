using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Vitarish.Intelligence.Api;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Application.LiveTranscription;
using Vitarish.Intelligence.Contracts;

namespace Intelligence.Tests.Api;

public sealed class RealtimeTranscriptionWebSocketSessionTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Valid_frames_emit_one_ordered_segment_and_one_completed_terminal()
    {
        FakeWebSocket socket = new([
            TextFrame("""{"type":"start","language":"fa-IR","mimeType":"audio/webm;codecs=opus"}"""),
            BinaryFrame([1, 2, 3]),
            TextFrame("""{"type":"stop"}""")
        ]);
        EchoProvider provider = new();

        await CreateSession(socket, provider).RunAsync(CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(2));

        RealtimeTranscriptionServerMessage[] messages = socket.SentMessages
            .Select(DeserializeMessage)
            .ToArray();
        messages.Select(message => message.Type).Should().Equal(
            RealtimeTranscriptionMessageTypes.Started,
            RealtimeTranscriptionMessageTypes.Segment,
            RealtimeTranscriptionMessageTypes.Completed);
        messages.Select(message => message.SessionId).Distinct().Should().ContainSingle();
        messages[1].Sequence.Should().Be(0);
        messages[1].Text.Should().Be("confirmed");
        messages.Count(IsTerminal).Should().Be(1);
        provider.ReceivedAudio.Should().Equal(new byte[] { 1, 2, 3 });
        socket.CloseStatus.Should().Be(WebSocketCloseStatus.NormalClosure);
    }

    [Fact]
    public async Task Invalid_first_frame_is_rejected_before_provider_work_starts()
    {
        FakeWebSocket socket = new([BinaryFrame([1, 2, 3])]);
        EchoProvider provider = new();

        await CreateSession(socket, provider).RunAsync(CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(2));

        RealtimeTranscriptionServerMessage message = socket.SentMessages
            .Select(DeserializeMessage)
            .Should()
            .ContainSingle()
            .Subject;
        message.Type.Should().Be(RealtimeTranscriptionMessageTypes.Failed);
        message.Error!.Code.Should().Be(RealtimeTranscriptionErrorCode.InvalidStart);
        provider.CallCount.Should().Be(0);
        socket.CloseStatus.Should().Be(WebSocketCloseStatus.PolicyViolation);
    }

    private static RealtimeTranscriptionWebSocketSession CreateSession(
        WebSocket socket,
        ITranscriptionProvider provider)
    {
        LiveTranscriptionSettings settings = new()
        {
            MaxFrameBytes = 2048,
            MaxTotalBytes = 4096,
            MaxDurationSeconds = 30,
            IdleTimeoutSeconds = 15,
            ProviderOperationTimeoutSeconds = 35,
            ChannelCapacity = 2
        };
        LiveTranscriptionSessionFactory factory = new(provider, settings, TimeProvider.System);
        return new RealtimeTranscriptionWebSocketSession(
            socket,
            factory,
            settings,
            NullLogger<RealtimeTranscriptionWebSocketSession>.Instance);
    }

    private static RealtimeTranscriptionServerMessage DeserializeMessage(byte[] payload) =>
        JsonSerializer.Deserialize<RealtimeTranscriptionServerMessage>(payload, JsonOptions)!;

    private static bool IsTerminal(RealtimeTranscriptionServerMessage message) =>
        message.Type is RealtimeTranscriptionMessageTypes.Completed
            or RealtimeTranscriptionMessageTypes.Failed
            or RealtimeTranscriptionMessageTypes.Canceled;

    private static FakeFrame TextFrame(string value) =>
        new(WebSocketMessageType.Text, Encoding.UTF8.GetBytes(value));

    private static FakeFrame BinaryFrame(byte[] value) =>
        new(WebSocketMessageType.Binary, value);

    private sealed class EchoProvider : ITranscriptionProvider
    {
        public bool IsAvailable => true;

        public int CallCount { get; private set; }

        public ConcurrentQueue<byte> ReceivedAudio { get; } = new();

        public async Task TranscribeAsync(
            ChannelReader<byte[]> audio,
            LiveTranscriptionProviderRequest request,
            Func<TranscriptionProviderEvent, CancellationToken, ValueTask> publish,
            CancellationToken cancellationToken)
        {
            CallCount++;
            await publish(TranscriptionProviderEvent.Ready(), cancellationToken);
            await foreach (byte[] frame in audio.ReadAllAsync(cancellationToken))
            {
                foreach (byte value in frame)
                {
                    ReceivedAudio.Enqueue(value);
                }

                await publish(
                    TranscriptionProviderEvent.FinalSegment("confirmed"),
                    cancellationToken);
            }
        }
    }

    private sealed record FakeFrame(WebSocketMessageType Type, byte[] Payload);

    private sealed class FakeWebSocket(IEnumerable<FakeFrame> frames) : WebSocket
    {
        private readonly Queue<FakeFrame> _frames = new(frames);
        private WebSocketState _state = WebSocketState.Open;
        private WebSocketCloseStatus? _closeStatus;
        private string? _closeDescription;

        public ConcurrentQueue<byte[]> SentMessages { get; } = new();

        public override WebSocketCloseStatus? CloseStatus => _closeStatus;

        public override string? CloseStatusDescription => _closeDescription;

        public override WebSocketState State => _state;

        public override string? SubProtocol => RealtimeTranscriptionProtocol.Subprotocol;

        public override void Abort() => _state = WebSocketState.Aborted;

        public override Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            SetClosed(closeStatus, statusDescription);
            return Task.CompletedTask;
        }

        public override Task CloseOutputAsync(
            WebSocketCloseStatus closeStatus,
            string? statusDescription,
            CancellationToken cancellationToken)
        {
            SetClosed(closeStatus, statusDescription);
            return Task.CompletedTask;
        }

        public override void Dispose() => _state = WebSocketState.Closed;

        public override Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FakeFrame frame = _frames.Dequeue();
            frame.Payload.AsSpan().CopyTo(buffer.AsSpan());
            return Task.FromResult(new WebSocketReceiveResult(
                frame.Payload.Length,
                frame.Type,
                true));
        }

        public override Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            messageType.Should().Be(WebSocketMessageType.Text);
            endOfMessage.Should().BeTrue();
            SentMessages.Enqueue(buffer.ToArray());
            return Task.CompletedTask;
        }

        private void SetClosed(WebSocketCloseStatus closeStatus, string? statusDescription)
        {
            _closeStatus = closeStatus;
            _closeDescription = statusDescription;
            _state = WebSocketState.Closed;
        }
    }
}
