using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Intelligence.Domain.Options;
using Vitarish.Intelligence.Infrastructure.Services.Agents;

namespace Intelligence.Tests.Agents;

public sealed class AgentRuntimeHttpRunnerTests
{
    private static readonly AgentDefinition Definition = new("test-model", "Follow the instructions.");
    private static readonly AgentInput Input = new("Do the task.");
    private static readonly AgentExecutionContext Context = new("execution-1", new Dictionary<string, string>
    {
        ["patientId"] = "patient-1"
    });
    private static readonly AgentExecutionLimits Limits = new(4, 4, 1_000, TimeSpan.FromSeconds(5));

    [Fact]
    public async Task RunAsync_SendsSignedRequestAndMapsRemoteToolExecutions()
    {
        JsonElement structuredOutput = JsonDocument.Parse("{\"answer\":42}").RootElement.Clone();
        const string completeHistory = "[{\"role\":\"assistant\",\"content\":\"finished\"}]";
        AgentRuntimeRunResponse response = new(
            true,
            "finished",
            structuredOutput,
            new(10, 3),
            2,
            1,
            [new("echo", "call-1", "tool-output")],
            History: completeHistory);
        RecordingHandler handler = new(response);
        AgentRuntimeHttpRunner runner = CreateRunner(handler);

        AgentRunResult result = await runner.RunAsync(
            Definition,
            Input with { History = "[{\"role\":\"user\",\"content\":\"prior\"}]" },
            [new RecordingTool()],
            Context,
            Limits);

        result.IsSuccess.Should().BeTrue();
        result.Output.Should().Be("finished");
        result.StructuredOutput!.Value.GetProperty("answer").GetInt32().Should().Be(42);
        result.ToolExecutions.Should().ContainSingle().Which.ToolName.Should().Be("echo");
        result.History.Should().Be(completeHistory);
        handler.Authorization.Should().Be("Bearer test-secret");
        handler.Request!.CallbackCredential.Should().Be("credential");
        handler.Request.Input.History.Should().Be("[{\"role\":\"user\",\"content\":\"prior\"}]");
        handler.Request.Context.ExecutionId.Should().Be(Context.ExecutionId);
        handler.Request.Context.Metadata!["patientId"].Should().Be("patient-1");
        handler.Request.Limits.Should().Be(Limits);
        handler.Request.Definition.ReasoningLevel.Should().Be(AgentReasoningLevel.Low);
    }

    [Fact]
    public async Task RunAsync_HidesRuntimeTransportFailure()
    {
        AgentRuntimeHttpRunner runner = CreateRunner(new RecordingHandler(
            new AgentRuntimeRunResponse(false, null, null, new(0, 0), 0, 0, [], Error: new(AgentRunErrorCode.ProviderFailure, "secret")),
            HttpStatusCode.BadGateway));

        AgentRunResult result = await runner.RunAsync(Definition, Input, [], Context, Limits);

        result.Error!.Code.Should().Be(AgentRunErrorCode.ProviderUnavailable);
        result.Error.Message.Should().NotContain("secret");
    }

    private static AgentRuntimeHttpRunner CreateRunner(RecordingHandler handler) => new(
        new HttpClient(handler) { BaseAddress = new Uri("http://agent-runtime") },
        Options.Create(new AgentRuntimeOptions
        {
            BaseUrl = "http://agent-runtime",
            ToolCallbackUrl = "http://clinic/internal/agent/tools/execute",
            SharedSecret = "test-secret"
        }),
        new TestCredentialService(),
        NullLogger<AgentRuntimeHttpRunner>.Instance);

    private sealed class TestCredentialService : IAgentRuntimeCredentialService
    {
        public string Create(AgentExecutionContext context, TimeSpan lifetime) => "credential";
        public bool Validate(string credential, AgentExecutionContext context) => credential == "credential";
    }

    private sealed class RecordingHandler(AgentRuntimeRunResponse response, HttpStatusCode statusCode = HttpStatusCode.OK)
        : HttpMessageHandler
    {
        public AgentRuntimeRunRequest? Request { get; private set; }
        public string? Authorization { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization?.ToString();
            Request = await request.Content!.ReadFromJsonAsync<AgentRuntimeRunRequest>(cancellationToken);
            HttpResponseMessage httpResponse = new(statusCode);
            if (statusCode == HttpStatusCode.OK) httpResponse.Content = JsonContent.Create(response);
            return httpResponse;
        }
    }

    private sealed class RecordingTool : IAgentTool
    {
        public AgentToolDefinition Definition { get; } = new("echo", "Echoes input.", "{\"type\":\"object\"}");

        public Task<AgentToolResult> ExecuteAsync(
            AgentToolCall call,
            AgentExecutionContext context,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new AgentToolResult(call.Id, "tool-output"));
    }
}
