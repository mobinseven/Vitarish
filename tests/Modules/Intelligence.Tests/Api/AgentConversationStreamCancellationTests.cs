using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vitarish.Intelligence.Api;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Contracts;

namespace Vitarish.Intelligence.Tests.Api;

/// <summary>
/// Drives the real conversation workflow behind the stream endpoint to prove that a server deadline and a
/// real client disconnect each reach the runtime and release the conversation lease.
/// </summary>
public sealed class AgentConversationStreamCancellationTests
{
    private const string AgentKey = "sample.agent";
    private const string SubjectKind = "tenant";
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);
    private static readonly RunAgentConversationRequest Request = new(AgentKey, SubjectKind, "subject-1", "hello");

    [Fact]
    public async Task A_runtime_deadline_during_provider_work_is_a_canceled_frame_and_releases_the_lease()
    {
        Harness harness = new(RuntimeBehavior.TimedOut);
        DefaultHttpContext context = new() { RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider() };
        MemoryStream body = new();
        context.Response.Body = body;

        IResult result = await AgentConversationStreamEndpoint.StreamAsync(
            Request,
            harness.Registry,
            NullLogger.Instance,
            TimeProvider.System,
            context,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(2),
            default);

        result.Should().BeSameAs(Results.Empty);
        Frames(body).Select(frame => frame.Kind).Should().Equal(
            StreamChunkKind.Started, StreamChunkKind.Progress, StreamChunkKind.Canceled);
        harness.LeaseReleased.Task.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task A_real_client_disconnect_during_a_silent_provider_interval_cancels_the_runtime_and_releases_the_lease()
    {
        Harness harness = new(RuntimeBehavior.Silent);
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using WebApplication app = builder.Build();
        app.MapPost("/stream", (HttpContext context, CancellationToken ct) =>
            AgentConversationStreamEndpoint.StreamAsync(
                Request,
                harness.Registry,
                NullLogger.Instance,
                TimeProvider.System,
                context,
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(2),
                ct));
        await app.StartAsync();
        string address = app.Urls.First();

        using HttpClient client = new() { BaseAddress = new Uri(address) };
        using HttpRequestMessage message = new(HttpMethod.Post, "/stream") { Content = JsonContent.Create(Request, options: Web) };
        using HttpResponseMessage response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
        using (Stream stream = await response.Content.ReadAsStreamAsync())
        using (StreamReader reader = new(stream, Encoding.UTF8))
        {
            string? started = await reader.ReadLineAsync().WaitAsync(Wait);
            JsonSerializer.Deserialize<IntelligenceStreamChunk<AgentTurnPayload>>(started!, Web)!.Kind.Should().Be(StreamChunkKind.Started);
            await harness.RuntimeEntered.Task.WaitAsync(Wait);
            harness.RuntimeCancelled.Task.IsCompleted.Should().BeFalse("the runtime is silent and the client is still connected");

            // Closing the connection is the only signal: the provider and tool interval writes nothing.
            response.Dispose();
        }

        await harness.RuntimeCancelled.Task.WaitAsync(Wait);
        await harness.LeaseReleased.Task.WaitAsync(Wait);
        await app.StopAsync();
    }

    private static List<IntelligenceStreamChunk<AgentTurnPayload>> Frames(MemoryStream body) =>
        Encoding.UTF8.GetString(body.ToArray())
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonSerializer.Deserialize<IntelligenceStreamChunk<AgentTurnPayload>>(line, Web)!)
            .ToList();

    private enum RuntimeBehavior
    {
        TimedOut,
        Silent
    }

    private sealed class Harness
    {
        public Harness(RuntimeBehavior behavior)
        {
            Mock<IAgentConversationStore> store = new();
            AgentConversationLease lease = new(Guid.CreateVersion7(), Guid.CreateVersion7(), 1, null);
            store.Setup(item => item.AcquireAsync(
                    It.IsAny<Guid?>(), It.IsAny<AgentConversationScope>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AgentConversationStoreResult(AgentConversationOutcome.Succeeded, lease));
            store.Setup(item => item.ReleaseAsync(It.IsAny<AgentConversationLease>(), It.IsAny<CancellationToken>()))
                .Callback(() => LeaseReleased.TrySetResult())
                .Returns(Task.CompletedTask);
            RunAgentConversationHandler handler = new(store.Object, new ScriptedRuntime(behavior, this));
            Registry = new AgentConversationEndpointRegistry([new WorkflowEndpoint(handler)]);
        }

        public IAgentConversationEndpointRegistry Registry { get; }
        public TaskCompletionSource RuntimeEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource RuntimeCancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource LeaseReleased { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class WorkflowEndpoint(RunAgentConversationHandler handler) : IAgentConversationEndpoint
    {
        public IReadOnlyCollection<AgentConversationRoute> Routes { get; } = [new(AgentKey, SubjectKind, StreamingEnabled: true)];

        public async Task<AgentConversationEndpointResult> RunAsync(AgentConversationEndpointRequest request, CancellationToken cancellationToken)
        {
            RunAgentConversationResult result = await handler.Handle(
                new RunAgentConversation(
                    null,
                    new AgentConversationScope("tenant-1", "user-1", AgentKey, SubjectKind, request.SubjectId),
                    new AgentDefinition("model", "instructions"),
                    new AgentInput(request.Message),
                    [],
                    new AgentExecutionContext("execution-1"),
                    new AgentExecutionLimits(Timeout: TimeSpan.FromSeconds(5)),
                    Observer: request.Observer),
                cancellationToken);
            return new AgentConversationEndpointResult(result.Outcome, "run-1", result.ConversationId);
        }
    }

    private sealed class ScriptedRuntime(RuntimeBehavior behavior, Harness harness) : IAgentRunner
    {
        public Task<AgentRunResult> RunAsync(
            AgentDefinition definition,
            AgentInput input,
            IReadOnlyCollection<IAgentTool> tools,
            AgentExecutionContext context,
            AgentExecutionLimits limits,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public async Task<AgentRunResult> RunStreamedAsync(
            AgentDefinition definition,
            AgentInput input,
            IReadOnlyCollection<IAgentTool> tools,
            AgentExecutionContext context,
            AgentExecutionLimits limits,
            AgentAnswerStream? answerStream,
            IAgentRunProgress progress,
            CancellationToken cancellationToken = default)
        {
            await progress.StageAsync(new(AgentTurnStage.ModelRoundStarted, 1), cancellationToken);
            harness.RuntimeEntered.TrySetResult();
            if (behavior == RuntimeBehavior.TimedOut)
                return Failure(AgentRunErrorCode.Timeout, "The agent execution timed out.");

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                harness.RuntimeCancelled.TrySetResult();
            }
            return Failure(AgentRunErrorCode.Cancelled, "The agent execution was cancelled.");
        }

        private static AgentRunResult Failure(AgentRunErrorCode code, string message) =>
            new(false, null, null, new(0, 0), TimeSpan.Zero, 0, 0, new(code, message));
    }
}
