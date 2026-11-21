using System.Net;
using System.Text;
using System.Threading.Channels;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.Services.Agents;

namespace Vitarish.Intelligence.Tests.Infrastructure;

public sealed class AgentRuntimeHttpRunnerStreamingTests
{
    private const string ResultFrame =
        "{\"kind\":\"result\",\"isSuccess\":true,\"output\":null,\"usage\":{\"inputTokens\":3,\"outputTokens\":2},\"iterations\":2,\"toolCalls\":1,\"toolExecutions\":[],\"history\":\"[]\"}\n";

    private static readonly AgentExecutionContext Context = new("execution-1", new Dictionary<string, string>
    {
        ["agentKey"] = "sample.agent",
        ["tenantId"] = "tenant-1",
        ["practiceId"] = "tenant-1",
        ["subjectKind"] = "tenant",
        ["subjectId"] = "subject-1",
        ["authenticatedUserId"] = "user-1",
        ["conversationId"] = "conversation-1",
        ["correlationId"] = "correlation-1",
        ["role"] = "runtime",
        ["locale"] = "en-US",
        ["timeZone"] = "UTC",
        ["calendar"] = "gregory",
        ["currentUtc"] = "2026-01-01T00:00:00Z",
        ["currentLocal"] = "2026-01-01T00:00:00Z",
        ["permissions"] = "[]"
    });

    [Fact]
    public async Task Stage_frames_are_reported_in_order_then_the_terminal_result_completes_the_run()
    {
        Handler handler = new(Body(
            "{\"kind\":\"stage\",\"stage\":\"ModelRoundStarted\",\"ordinal\":1}\n" +
            "{\"kind\":\"stage\",\"stage\":\"ToolStarted\",\"ordinal\":1}\n" +
            "{\"kind\":\"stage\",\"stage\":\"ToolCompleted\",\"ordinal\":1}\n" +
            "{\"kind\":\"stage\",\"stage\":\"ModelRoundStarted\",\"ordinal\":2}\n" +
            ResultFrame));
        Progress progress = new();

        AgentRunResult run = await Runner(handler).RunStreamedAsync(Definition, new("hi"), [], Context, new(), progress);

        run.IsSuccess.Should().BeTrue();
        run.Iterations.Should().Be(2);
        run.History.Should().Be("[]");
        progress.Stages.Should().Equal(
            new AgentTurnStageEvent(AgentTurnStage.ModelRoundStarted, 1),
            new AgentTurnStageEvent(AgentTurnStage.ToolStarted, 1),
            new AgentTurnStageEvent(AgentTurnStage.ToolCompleted, 1),
            new AgentTurnStageEvent(AgentTurnStage.ModelRoundStarted, 2));
        handler.Paths.Should().Equal(AgentRuntimeHttpRunner.StreamPath);
    }

    [Fact]
    public async Task Stage_frames_are_delivered_before_the_stream_ends()
    {
        GatedStream stream = new();
        Handler handler = new(stream);
        Progress progress = new();
        Task<AgentRunResult> run = Runner(handler).RunStreamedAsync(Definition, new("hi"), [], Context, new(), progress);

        stream.Send("{\"kind\":\"stage\",\"stage\":\"ModelRoundStarted\",\"ordinal\":1}\n");
        await progress.WaitForAsync(1);
        run.IsCompleted.Should().BeFalse("the response must be read as it arrives, not buffered to the end");

        stream.Send(ResultFrame);
        stream.Complete();
        (await run).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Stream_ending_without_a_terminal_frame_is_an_unknown_outcome_failure_that_is_never_replayed()
    {
        Handler handler = new(Body("{\"kind\":\"stage\",\"stage\":\"ToolStarted\",\"ordinal\":1}\n"));

        AgentRunResult run = await Runner(handler).RunStreamedAsync(Definition, new("hi"), [], Context, new(), new Progress());

        run.IsSuccess.Should().BeFalse();
        run.Error!.Code.Should().Be(AgentRunErrorCode.ProviderFailure, "InvalidProviderResponse would allow a history retry after a tool may have run");
        handler.Paths.Should().ContainSingle("a disconnect never triggers an automatic retry or buffered fallback");
    }

    [Fact]
    public async Task Stream_ending_inside_a_frame_is_a_failure()
    {
        Handler handler = new(Body(ResultFrame.TrimEnd('\n')));

        AgentRunResult run = await Runner(handler).RunStreamedAsync(Definition, new("hi"), [], Context, new(), new Progress());

        run.Error!.Code.Should().Be(AgentRunErrorCode.ProviderFailure);
    }

    [Theory]
    [InlineData("{\"kind\":\"stage\",\"stage\":\"Unknown\",\"ordinal\":1}\n")]
    [InlineData("{\"kind\":\"stage\",\"stage\":\"ToolStarted\",\"ordinal\":-1}\n")]
    [InlineData("{\"kind\":\"stage\",\"stage\":\"ToolStarted\"}\n")]
    [InlineData("{\"kind\":\"other\"}\n")]
    [InlineData("{\"stage\":\"ToolStarted\",\"ordinal\":1}\n")]
    [InlineData("not json\n")]
    [InlineData("[1]\n")]
    public async Task Malformed_or_unknown_frames_fail_the_run_instead_of_being_skipped(string frame)
    {
        AgentRunResult run = await Runner(new Handler(Body(frame + ResultFrame)))
            .RunStreamedAsync(Definition, new("hi"), [], Context, new(), new Progress());

        run.IsSuccess.Should().BeFalse();
        run.Error!.Code.Should().Be(AgentRunErrorCode.ProviderFailure);
    }

    [Fact]
    public async Task Oversized_frames_are_rejected()
    {
        string oversizedStage = "{\"kind\":\"stage\",\"stage\":\"ToolStarted\",\"ordinal\":1,\"pad\":\"" + new string('x', AgentRuntimeHttpRunner.MaxStageFrameBytes) + "\"}\n";
        string oversizedResult = new string('y', AgentRuntimeHttpRunner.MaxFrameBytes + 1) + "\n";

        AgentRunResult stage = await Runner(new Handler(Body(oversizedStage + ResultFrame)))
            .RunStreamedAsync(Definition, new("hi"), [], Context, new(), new Progress());
        AgentRunResult result = await Runner(new Handler(Body(oversizedResult)))
            .RunStreamedAsync(Definition, new("hi"), [], Context, new(), new Progress());

        stage.Error!.Code.Should().Be(AgentRunErrorCode.ProviderFailure);
        result.Error!.Code.Should().Be(AgentRunErrorCode.ProviderFailure);
    }

    [Fact]
    public async Task A_frame_after_the_terminal_result_fails_the_run()
    {
        AgentRunResult run = await Runner(new Handler(Body(ResultFrame + "{\"kind\":\"stage\",\"stage\":\"ToolStarted\",\"ordinal\":1}\n")))
            .RunStreamedAsync(Definition, new("hi"), [], Context, new(), new Progress());

        run.Error!.Code.Should().Be(AgentRunErrorCode.ProviderFailure);
    }

    [Fact]
    public async Task A_runtime_error_status_is_unavailable_without_falling_back_to_the_buffered_route()
    {
        Handler handler = new(Body(string.Empty), HttpStatusCode.NotFound);

        AgentRunResult run = await Runner(handler).RunStreamedAsync(Definition, new("hi"), [], Context, new(), new Progress());

        run.Error!.Code.Should().Be(AgentRunErrorCode.ProviderUnavailable);
        handler.Paths.Should().Equal(AgentRuntimeHttpRunner.StreamPath);
    }

    [Fact]
    public async Task Cancellation_stops_the_read_and_reports_cancelled()
    {
        GatedStream stream = new();
        using CancellationTokenSource cancellation = new();
        Task<AgentRunResult> run = Runner(new Handler(stream))
            .RunStreamedAsync(Definition, new("hi"), [], Context, new(), new Progress(), cancellation.Token);

        await cancellation.CancelAsync();

        (await run).Error!.Code.Should().Be(AgentRunErrorCode.Cancelled);
    }

    [Fact]
    public async Task The_whole_turn_deadline_bounds_a_stream_that_never_finishes()
    {
        GatedStream stream = new();

        AgentRunResult run = await Runner(new Handler(stream))
            .RunStreamedAsync(Definition, new("hi"), [], Context, new(Timeout: TimeSpan.FromMilliseconds(50)), new Progress());

        run.Error!.Code.Should().Be(AgentRunErrorCode.Timeout);
    }

    [Fact]
    public async Task A_slow_progress_consumer_applies_backpressure_before_the_next_frame_is_read()
    {
        GatedStream stream = new();
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Progress progress = new() { Gate = release.Task };
        Task<AgentRunResult> run = Runner(new Handler(stream)).RunStreamedAsync(Definition, new("hi"), [], Context, new(), progress);

        stream.Send("{\"kind\":\"stage\",\"stage\":\"ToolStarted\",\"ordinal\":1}\n" + ResultFrame);
        stream.Complete();
        await progress.WaitForAsync(1);
        run.IsCompleted.Should().BeFalse("the terminal frame is not consumed while the stage report is outstanding");

        release.SetResult();
        (await run).IsSuccess.Should().BeTrue();
    }

    private static readonly AgentDefinition Definition = new("model", "instructions");

    private static AgentRuntimeHttpRunner Runner(Handler handler)
    {
        Mock<IAgentRuntimeCredentialService> credentials = new();
        credentials.Setup(service => service.Create(It.IsAny<AgentRuntimeExecutionEnvelope>())).Returns("credential");
        HttpClient client = new(handler) { BaseAddress = new Uri("http://runtime.test") };
        return new AgentRuntimeHttpRunner(
            client,
            Options.Create(new AgentRuntimeOptions { BaseUrl = "http://runtime.test", ToolCallbackUrl = "http://main.test/callback", SharedSecret = "secret" }),
            credentials.Object,
            NullLogger<AgentRuntimeHttpRunner>.Instance);
    }

    private static MemoryStream Body(string text) => new(Encoding.UTF8.GetBytes(text));

    private sealed class Progress : IAgentRunProgress
    {
        private readonly Channel<AgentTurnStageEvent> received = Channel.CreateUnbounded<AgentTurnStageEvent>();
        public List<AgentTurnStageEvent> Stages { get; } = [];
        public Task? Gate { get; init; }

        public async ValueTask StageAsync(AgentTurnStageEvent stage, CancellationToken cancellationToken)
        {
            Stages.Add(stage);
            received.Writer.TryWrite(stage);
            if (Gate is not null)
                await Gate.WaitAsync(cancellationToken);
        }

        public async Task WaitForAsync(int count)
        {
            while (Stages.Count < count)
                _ = await received.Reader.ReadAsync();
        }
    }

    private sealed class Handler(Stream body, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StreamContent(body) });
        }
    }

    /// <summary>A response body the test feeds frame by frame; it ends when completed or when the read is cancelled.</summary>
    private sealed class GatedStream : Stream
    {
        private readonly Channel<byte[]?> chunks = Channel.CreateUnbounded<byte[]?>();
        private byte[] current = [];
        private int offset;

        public void Send(string text) => chunks.Writer.TryWrite(Encoding.UTF8.GetBytes(text));
        public void Complete() => chunks.Writer.TryWrite(null);

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (offset >= current.Length)
            {
                byte[]? next = await chunks.Reader.ReadAsync(cancellationToken);
                if (next is null)
                    return 0;
                current = next;
                offset = 0;
            }
            int count = Math.Min(buffer.Length, current.Length - offset);
            current.AsMemory(offset, count).CopyTo(buffer);
            offset += count;
            return count;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
