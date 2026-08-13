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
        ["tenantId"] = "practice-1",
        ["practiceId"] = "practice-1",
        ["patientId"] = "11111111-1111-1111-1111-111111111111",
        ["subjectKind"] = "patient",
        ["subjectId"] = "11111111-1111-1111-1111-111111111111",
        ["authenticatedUserId"] = "user-1",
        ["conversationId"] = "conversation-1",
        ["agentKey"] = "clinic.assistant.patient",
        ["permissions"] = "[\"Patient.Read\"]",
        ["role"] = "Clinician",
        ["locale"] = "en-US",
        ["timeZone"] = "UTC",
        ["calendar"] = "gregory",
        ["currentUtc"] = "2026-07-29T12:00:00.0000000+00:00",
        ["currentLocal"] = "2026-07-29T12:00:00.0000000+00:00",
        ["correlationId"] = "execution-1"
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
            [new("echo", "call-1", "tool-output", false, "{\"value\":42}")],
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
        result.ToolExecutions.Single().ArgumentsJson.Should().Be("{\"value\":42}");
        result.History.Should().Be(completeHistory);
        handler.Authorization.Should().Be("Bearer test-secret");
        handler.Request!.CallbackCredential.Should().Be("credential");
        handler.Request.Input.History.Should().Be("[{\"role\":\"user\",\"content\":\"prior\"}]");
        handler.Request.ExecutionEnvelope.ExecutionId.Should().Be(Context.ExecutionId);
        handler.Request.ExecutionEnvelope.SubjectId.Should().Be("11111111-1111-1111-1111-111111111111");
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
        public string Create(AgentRuntimeExecutionEnvelope envelope) => "credential";
        public bool Validate(string credential, AgentRuntimeExecutionEnvelope envelope) => credential == "credential";
        public bool ValidateCallback(string credential, AgentRuntimeExecutionEnvelope envelope) => credential == "credential";
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
