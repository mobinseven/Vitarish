using System.Collections.Concurrent;
using FluentAssertions;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Infrastructure.Services.OpenAi.Agents;

namespace Intelligence.Tests.Agents;

public sealed class OpenAiAgentRunnerTests
{
    private static readonly AgentDefinition Definition = new("test-model", "Follow the instructions.");
    private static readonly AgentInput Input = new("Do the task.");
    private static readonly AgentExecutionContext Context = new("execution-1");
    private static readonly AgentExecutionLimits Limits = new(4, 4, 1_000, TimeSpan.FromSeconds(5));

    [Fact]
    public async Task RunAsync_ReturnsFinalResponseWithoutTools()
    {
        FakeClient client = new(new OpenAiAgentResponse("response-1", "finished", [], 10, 3));
        OpenAiAgentRunner runner = CreateRunner(client);

        AgentRunResult result = await runner.RunAsync(Definition, Input, [], Context, Limits);

        result.IsSuccess.Should().BeTrue();
        result.Output.Should().Be("finished");
        result.Usage.Should().Be(new AgentRunUsage(10, 3));
        result.Iterations.Should().Be(1);
        client.Requests.Single().Tools.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_DispatchesOneRegisteredToolAndReturnsItsResultToProvider()
    {
        FakeClient client = new(
            new OpenAiAgentResponse("response-1", null, [new("call-1", "echo", "{\"value\":\"hello\"}")], 5, 2),
            new OpenAiAgentResponse("response-2", "done", [], 6, 2));
        RecordingTool tool = new("echo");
        OpenAiAgentRunner runner = CreateRunner(client);

        AgentRunResult result = await runner.RunAsync(Definition, Input, [tool], Context, Limits);

        result.IsSuccess.Should().BeTrue();
        result.ToolCalls.Should().Be(1);
        tool.Calls.Should().ContainSingle().Which.Name.Should().Be("echo");
        client.Requests[0].Tools.Should().ContainSingle().Which.Name.Should().Be("echo");
        client.Requests[1].PreviousResponseId.Should().Be("response-1");
        client.Requests[1].UserInput.Should().BeNull();
        client.Requests[1].ToolResults.Should().ContainSingle().Which.Output.Should().Be("tool-output");
    }

    [Fact]
    public async Task RunAsync_SupportsMultipleSequentialToolInvocations()
    {
        FakeClient client = new(
            new OpenAiAgentResponse("response-1", null, [new("call-1", "echo", "{}")], 1, 1),
            new OpenAiAgentResponse("response-2", null, [new("call-2", "echo", "{}")], 1, 1),
            new OpenAiAgentResponse("response-3", "done", [], 1, 1));
        RecordingTool tool = new("echo");

        AgentRunResult result = await CreateRunner(client).RunAsync(Definition, Input, [tool], Context, Limits);

        result.IsSuccess.Should().BeTrue();
        result.Iterations.Should().Be(3);
        result.ToolCalls.Should().Be(2);
        tool.Calls.Select(static call => call.Id).Should().Equal("call-1", "call-2");
    }

    [Fact]
    public async Task RunAsync_RejectsUnknownToolWithoutDispatching()
    {
        FakeClient client = new(new OpenAiAgentResponse(
            "response-1", null, [new("call-1", "unregistered", "{}")], 1, 1));

        AgentRunResult result = await CreateRunner(client).RunAsync(
            Definition, Input, [new RecordingTool("echo")], Context, Limits);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be(AgentRunErrorCode.UnknownTool);
    }

    [Fact]
    public async Task RunAsync_RejectsMalformedToolArgumentsWithoutDispatching()
    {
        RecordingTool tool = new("echo");
        FakeClient client = new(new OpenAiAgentResponse(
            "response-1", null, [new("call-1", "echo", "not-json")], 1, 1));

        AgentRunResult result = await CreateRunner(client).RunAsync(Definition, Input, [tool], Context, Limits);

        result.Error!.Code.Should().Be(AgentRunErrorCode.MalformedToolArguments);
        tool.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_RejectsNonObjectToolArgumentsWithoutDispatching()
    {
        RecordingTool tool = new("echo");
        FakeClient client = new(new OpenAiAgentResponse(
            "response-1", null, [new("call-1", "echo", "[]")], 1, 1));

        AgentRunResult result = await CreateRunner(client).RunAsync(Definition, Input, [tool], Context, Limits);

        result.Error!.Code.Should().Be(AgentRunErrorCode.MalformedToolArguments);
        tool.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_RejectsInvalidSchemasBeforeCallingProvider()
    {
        RecordingTool tool = new("echo", "not-json");
        AgentDefinition definition = Definition with
        {
            Result = new AgentResultDefinition("answer", "[]")
        };
        FakeClient client = new(new OpenAiAgentResponse("unused", "unused", [], 1, 1));

        AgentRunResult toolResult = await CreateRunner(client).RunAsync(Definition, Input, [tool], Context, Limits);
        AgentRunResult resultDefinitionResult = await CreateRunner(client).RunAsync(definition, Input, [], Context, Limits);

        toolResult.Error!.Code.Should().Be(AgentRunErrorCode.InvalidRequest);
        resultDefinitionResult.Error!.Code.Should().Be(AgentRunErrorCode.InvalidRequest);
        client.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_EnforcesIterationLimit()
    {
        FakeClient client = new(
            new OpenAiAgentResponse("response-1", null, [new("call-1", "echo", "{}")], 1, 1),
            new OpenAiAgentResponse("response-2", null, [new("call-2", "echo", "{}")], 1, 1));

        AgentRunResult result = await CreateRunner(client).RunAsync(
            Definition, Input, [new RecordingTool("echo")], Context, Limits with { MaxIterations = 2 });

        result.Error!.Code.Should().Be(AgentRunErrorCode.IterationLimitExceeded);
        client.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task RunAsync_EnforcesToolCallLimitBeforeDispatch()
    {
        RecordingTool tool = new("echo");
        FakeClient client = new(new OpenAiAgentResponse(
            "response-1", null,
            [new("call-1", "echo", "{}"), new("call-2", "echo", "{}")], 1, 1));

        AgentRunResult result = await CreateRunner(client).RunAsync(
            Definition, Input, [tool], Context, Limits with { MaxToolCalls = 1 });

        result.Error!.Code.Should().Be(AgentRunErrorCode.ToolCallLimitExceeded);
        tool.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task RunAsync_EnforcesTokenLimit()
    {
        FakeClient client = new(new OpenAiAgentResponse("response-1", "done", [], 6, 5));

        AgentRunResult result = await CreateRunner(client).RunAsync(
            Definition, Input, [], Context, Limits with { MaxTokens = 10 });

        result.Error!.Code.Should().Be(AgentRunErrorCode.TokenLimitExceeded);
    }

    [Fact]
    public async Task RunAsync_MapsCallerCancellation()
    {
        FakeClient client = new(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException();
        });
        using CancellationTokenSource source = new();
        source.Cancel();

        AgentRunResult result = await CreateRunner(client).RunAsync(
            Definition, Input, [], Context, Limits, source.Token);

        result.Error!.Code.Should().Be(AgentRunErrorCode.Cancelled);
    }

    [Fact]
    public async Task RunAsync_EnforcesTimeout()
    {
        FakeClient client = new(async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException();
        });

        AgentRunResult result = await CreateRunner(client).RunAsync(
            Definition,
            Input,
            [],
            Context,
            Limits with { Timeout = TimeSpan.FromMilliseconds(20) });

        result.Error!.Code.Should().Be(AgentRunErrorCode.Timeout);
    }

    [Fact]
    public async Task RunAsync_MapsProviderFailureWithoutLeakingDetails()
    {
        FakeClient client = new(_ => throw new InvalidOperationException("sensitive provider detail"));

        AgentRunResult result = await CreateRunner(client).RunAsync(Definition, Input, [], Context, Limits);

        result.Error!.Code.Should().Be(AgentRunErrorCode.ProviderFailure);
        result.Error.Message.Should().NotContain("sensitive");
    }

    [Fact]
    public async Task RunAsync_ParsesStructuredResult()
    {
        AgentDefinition structuredDefinition = Definition with
        {
            Result = new AgentResultDefinition("answer", "{\"type\":\"object\"}")
        };
        FakeClient client = new(new OpenAiAgentResponse("response-1", "{\"answer\":42}", [], 1, 1));

        AgentRunResult result = await CreateRunner(client).RunAsync(
            structuredDefinition, Input, [], Context, Limits);

        result.IsSuccess.Should().BeTrue();
        result.StructuredOutput!.Value.GetProperty("answer").GetInt32().Should().Be(42);
    }

    private static OpenAiAgentRunner CreateRunner(IOpenAiAgentClient client) =>
        new(new RegisteredAgentToolDispatcher(), client);

    private sealed class FakeClient : IOpenAiAgentClient
    {
        private readonly Queue<Func<CancellationToken, Task<OpenAiAgentResponse>>> responses;

        public FakeClient(params OpenAiAgentResponse[] responses)
            : this(responses.Select(response =>
                new Func<CancellationToken, Task<OpenAiAgentResponse>>(_ => Task.FromResult(response))).ToArray())
        {
        }

        public FakeClient(params Func<CancellationToken, Task<OpenAiAgentResponse>>[] responses) =>
            this.responses = new Queue<Func<CancellationToken, Task<OpenAiAgentResponse>>>(responses);

        public List<OpenAiAgentRequest> Requests { get; } = [];

        public async Task<OpenAiAgentResponse> CreateResponseAsync(
            OpenAiAgentRequest request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return await responses.Dequeue()(cancellationToken);
        }
    }

    private sealed class RecordingTool(
        string name,
        string parametersJsonSchema = "{\"type\":\"object\",\"additionalProperties\":false}") : IAgentTool
    {
        public AgentToolDefinition Definition { get; } = new(
            name,
            "Returns a test response.",
            parametersJsonSchema);

        public ConcurrentQueue<AgentToolCall> Calls { get; } = [];

        public Task<AgentToolResult> ExecuteAsync(
            AgentToolCall call,
            AgentExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            Calls.Enqueue(call);
            return Task.FromResult(new AgentToolResult(call.Id, "tool-output"));
        }
    }
}
