using System.Collections.Concurrent;
using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Vitarish.Intelligence.Application.Abstractions;
using Vitarish.Intelligence.Application.LiveTranscription;

namespace Intelligence.Tests.Application;

public sealed class LiveTranscriptionSessionTests
{
    [Fact]
    public async Task Session_orders_final_segments_and_completes_only_after_explicit_stop()
    {
        FakeTranscriptionProvider provider = new()
        {
            SegmentForFrame = frame => frame[0] == 1 ? "first" : "second"
        };
        ConcurrentQueue<LiveTranscriptionSegment> segments = new();
        LiveTranscriptionSession session = CreateSession(provider, segment => segments.Enqueue(segment));

        (await session.Ready.WaitAsync(TimeSpan.FromSeconds(1))).Should().BeTrue();
        (await session.AcceptAudioAsync(new byte[] { 1 }, CancellationToken.None)).Accepted.Should().BeTrue();
        (await session.AcceptAudioAsync(new byte[] { 2 }, CancellationToken.None)).Accepted.Should().BeTrue();

        session.Completion.IsCompleted.Should().BeFalse();
        session.RequestStop();
        LiveTranscriptionOutcome outcome = await session.Completion.WaitAsync(TimeSpan.FromSeconds(1));

        outcome.Kind.Should().Be(LiveTranscriptionOutcomeKind.Completed);
        segments.Should().Equal(
            new LiveTranscriptionSegment(0, "first"),
            new LiveTranscriptionSegment(1, "second"));
        session.State.Should().Be(LiveTranscriptionSessionState.Completed);
        await session.DisposeAsync();
    }

    [Fact]
    public async Task Session_rejects_an_oversized_frame_with_one_stable_failure()
    {
        FakeTranscriptionProvider provider = new();
        LiveTranscriptionSettings settings = CreateSettings();
        settings.MaxFrameBytes = 1024;
        LiveTranscriptionSession session = CreateSession(provider, _ => { }, settings: settings);
        (await session.Ready.WaitAsync(TimeSpan.FromSeconds(1))).Should().BeTrue();

        LiveTranscriptionAcceptResult accepted = await session.AcceptAudioAsync(
            new byte[1025],
            CancellationToken.None);
        LiveTranscriptionOutcome outcome = await session.Completion.WaitAsync(TimeSpan.FromSeconds(1));

        accepted.Accepted.Should().BeFalse();
        accepted.Error!.Code.Should().Be(LiveTranscriptionErrorCode.FrameTooLarge);
        outcome.Should().BeEquivalentTo(new LiveTranscriptionOutcome(
            LiveTranscriptionOutcomeKind.Failed,
            accepted.Error));
        await session.DisposeAsync();
    }

    [Fact]
    public async Task Session_rejects_audio_after_the_total_payload_limit()
    {
        FakeTranscriptionProvider provider = new();
        LiveTranscriptionSettings settings = CreateSettings();
        settings.MaxFrameBytes = 1024;
        settings.MaxTotalBytes = 1024;
        LiveTranscriptionSession session = CreateSession(provider, _ => { }, settings: settings);
        (await session.Ready.WaitAsync(TimeSpan.FromSeconds(1))).Should().BeTrue();

        (await session.AcceptAudioAsync(new byte[600], CancellationToken.None)).Accepted
            .Should().BeTrue();
        LiveTranscriptionAcceptResult accepted = await session.AcceptAudioAsync(
            new byte[500],
            CancellationToken.None);
        LiveTranscriptionOutcome outcome = await session.Completion.WaitAsync(TimeSpan.FromSeconds(1));

        accepted.Accepted.Should().BeFalse();
        accepted.Error!.Code.Should().Be(LiveTranscriptionErrorCode.PayloadTooLarge);
        outcome.Error!.Code.Should().Be(LiveTranscriptionErrorCode.PayloadTooLarge);
        await session.DisposeAsync();
    }

    [Fact]
    public async Task Session_preserves_received_segments_when_the_provider_fails()
    {
        FakeTranscriptionProvider provider = new()
        {
            SegmentForFrame = _ => "confirmed",
            FailureAfterFirstFrame = new TranscriptionProviderException(
                LiveTranscriptionErrorCode.ProviderUnavailable,
                "Live transcription provider is unavailable.")
        };
        ConcurrentQueue<LiveTranscriptionSegment> segments = new();
        LiveTranscriptionSession session = CreateSession(provider, segment => segments.Enqueue(segment));
        (await session.Ready.WaitAsync(TimeSpan.FromSeconds(1))).Should().BeTrue();

        _ = await session.AcceptAudioAsync(new byte[] { 1 }, CancellationToken.None);
        LiveTranscriptionOutcome outcome = await session.Completion.WaitAsync(TimeSpan.FromSeconds(1));

        segments.Should().Equal(new LiveTranscriptionSegment(0, "confirmed"));
        outcome.Kind.Should().Be(LiveTranscriptionOutcomeKind.Failed);
        outcome.Error!.Code.Should().Be(LiveTranscriptionErrorCode.ProviderUnavailable);
        await session.DisposeAsync();
    }

    [Fact]
    public async Task Session_cancellation_propagates_to_the_provider_and_returns_canceled()
    {
        using CancellationTokenSource cancellation = new();
        FakeTranscriptionProvider provider = new();
        LiveTranscriptionSession session = CreateSession(
            provider,
            _ => { },
            cancellationToken: cancellation.Token);
        (await session.Ready.WaitAsync(TimeSpan.FromSeconds(1))).Should().BeTrue();

        cancellation.Cancel();
        LiveTranscriptionOutcome outcome = await session.Completion.WaitAsync(TimeSpan.FromSeconds(1));

        outcome.Kind.Should().Be(LiveTranscriptionOutcomeKind.Canceled);
        provider.CancellationObserved.Should().BeTrue();
        await session.DisposeAsync();
    }

    [Fact]
    public async Task Already_canceled_session_never_invokes_the_provider()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        FakeTranscriptionProvider provider = new();
        LiveTranscriptionSession session = CreateSession(
            provider,
            _ => { },
            cancellationToken: cancellation.Token);

        LiveTranscriptionOutcome outcome = await session.Completion.WaitAsync(TimeSpan.FromSeconds(1));

        outcome.Kind.Should().Be(LiveTranscriptionOutcomeKind.Canceled);
        (await session.Ready).Should().BeFalse();
        provider.CallCount.Should().Be(0);
        await session.DisposeAsync();
    }

    [Fact]
    public async Task Session_fails_deterministically_after_network_idle()
    {
        FakeTimeProvider time = new();
        FakeTranscriptionProvider provider = new();
        LiveTranscriptionSettings settings = CreateSettings();
        settings.IdleTimeoutSeconds = 5;
        settings.MaxDurationSeconds = 10;
        settings.ProviderOperationTimeoutSeconds = 20;
        LiveTranscriptionSession session = CreateSession(provider, _ => { }, settings, time);
        (await session.Ready.WaitAsync(TimeSpan.FromSeconds(1))).Should().BeTrue();

        time.Advance(TimeSpan.FromSeconds(6));
        LiveTranscriptionOutcome outcome = await session.Completion.WaitAsync(TimeSpan.FromSeconds(1));

        outcome.Kind.Should().Be(LiveTranscriptionOutcomeKind.Failed);
        outcome.Error!.Code.Should().Be(LiveTranscriptionErrorCode.IdleTimeout);
        await session.DisposeAsync();
    }

    [Fact]
    public async Task Session_fails_at_maximum_duration_even_when_audio_is_recent()
    {
        FakeTimeProvider time = new();
        FakeTranscriptionProvider provider = new();
        LiveTranscriptionSettings settings = CreateSettings();
        settings.IdleTimeoutSeconds = 4;
        settings.MaxDurationSeconds = 5;
        settings.ProviderOperationTimeoutSeconds = 10;
        LiveTranscriptionSession session = CreateSession(provider, _ => { }, settings, time);
        (await session.Ready.WaitAsync(TimeSpan.FromSeconds(1))).Should().BeTrue();
        await Task.Yield();

        time.Advance(TimeSpan.FromSeconds(3));
        _ = await session.AcceptAudioAsync(new byte[] { 1 }, CancellationToken.None);
        time.Advance(TimeSpan.FromSeconds(3));
        LiveTranscriptionOutcome outcome = await session.Completion.WaitAsync(TimeSpan.FromSeconds(1));

        outcome.Kind.Should().Be(LiveTranscriptionOutcomeKind.Failed);
        outcome.Error!.Code.Should().Be(LiveTranscriptionErrorCode.DurationLimitExceeded);
        await session.DisposeAsync();
    }

    [Fact]
    public async Task Session_times_out_a_provider_that_never_becomes_ready()
    {
        FakeTimeProvider time = new();
        FakeTranscriptionProvider provider = new() { AnnounceReady = false };
        LiveTranscriptionSettings settings = CreateSettings();
        settings.IdleTimeoutSeconds = 2;
        settings.MaxDurationSeconds = 4;
        settings.ProviderOperationTimeoutSeconds = 5;
        LiveTranscriptionSession session = CreateSession(provider, _ => { }, settings, time);

        time.Advance(TimeSpan.FromSeconds(6));
        LiveTranscriptionOutcome outcome = await session.Completion.WaitAsync(TimeSpan.FromSeconds(1));

        outcome.Kind.Should().Be(LiveTranscriptionOutcomeKind.Failed);
        outcome.Error!.Code.Should().Be(LiveTranscriptionErrorCode.ProviderTimeout);
        (await session.Ready).Should().BeFalse();
        await session.DisposeAsync();
    }

    [Theory]
    [InlineData("de-DE", "audio/webm;codecs=opus", LiveTranscriptionErrorCode.UnsupportedLanguage)]
    [InlineData("fa-IR", "audio/ogg", LiveTranscriptionErrorCode.UnsupportedMediaType)]
    public void Factory_rejects_unadvertised_start_values(
        string language,
        string mimeType,
        LiveTranscriptionErrorCode expectedCode)
    {
        LiveTranscriptionSettings settings = CreateSettings();
        LiveTranscriptionSessionFactory factory = new(
            new FakeTranscriptionProvider(),
            settings,
            TimeProvider.System);

        LiveTranscriptionSessionCreationResult result = factory.Create(
            new LiveTranscriptionRequest(language, mimeType),
            (_, _) => ValueTask.CompletedTask,
            CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Session.Should().BeNull();
        result.Error!.Code.Should().Be(expectedCode);
    }

    private static LiveTranscriptionSession CreateSession(
        FakeTranscriptionProvider provider,
        Action<LiveTranscriptionSegment> onSegment,
        LiveTranscriptionSettings? settings = null,
        TimeProvider? timeProvider = null,
        CancellationToken cancellationToken = default)
    {
        LiveTranscriptionSessionCreationResult result = new LiveTranscriptionSessionFactory(
                provider,
                settings ?? CreateSettings(),
                timeProvider ?? TimeProvider.System)
            .Create(
                new LiveTranscriptionRequest("fa-IR", "audio/webm;codecs=opus"),
                (segment, _) =>
                {
                    onSegment(segment);
                    return ValueTask.CompletedTask;
                },
                cancellationToken);

        result.Succeeded.Should().BeTrue();
        return result.Session!;
    }

    private static LiveTranscriptionSettings CreateSettings() => new()
    {
        MaxFrameBytes = 2048,
        MaxTotalBytes = 4096,
        MaxDurationSeconds = 30,
        IdleTimeoutSeconds = 15,
        ProviderOperationTimeoutSeconds = 35,
        ChannelCapacity = 2
    };

    private sealed class FakeTranscriptionProvider : ITranscriptionProvider
    {
        public bool IsAvailable { get; set; } = true;

        public bool AnnounceReady { get; init; } = true;

        public Func<byte[], string?>? SegmentForFrame { get; init; }

        public TranscriptionProviderException? FailureAfterFirstFrame { get; init; }

        public bool CancellationObserved { get; private set; }

        public int CallCount { get; private set; }

        public async Task TranscribeAsync(
            ChannelReader<byte[]> audio,
            LiveTranscriptionProviderRequest request,
            Func<TranscriptionProviderEvent, CancellationToken, ValueTask> publish,
            CancellationToken cancellationToken)
        {
            CallCount++;
            if (AnnounceReady)
            {
                await publish(TranscriptionProviderEvent.Ready(), cancellationToken);
            }
            try
            {
                await foreach (byte[] frame in audio.ReadAllAsync(cancellationToken))
                {
                    string? segment = SegmentForFrame?.Invoke(frame);
                    if (segment is not null)
                    {
                        await publish(TranscriptionProviderEvent.FinalSegment(segment), cancellationToken);
                    }

                    if (FailureAfterFirstFrame is not null)
                    {
                        throw FailureAfterFirstFrame;
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                CancellationObserved = true;
                throw;
            }
        }
    }
}
