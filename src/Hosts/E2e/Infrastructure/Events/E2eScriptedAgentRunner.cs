using System.Text.Json;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.E2e.Infrastructure.Events;

/// <summary>
/// Stands in for the model provider in the E2E host. A scenario sends a user message that starts with
/// <see cref="ScriptPrefix"/> followed by a JSON script: the tool calls the "model" makes and its structured result.
/// The calls run through the same guarded tools a real run would call back into, so tool authorization, result
/// parsing and prepared-action persistence are exercised for real. Any other message fails like an unreachable
/// provider, as the E2E host has no model.
/// </summary>
internal sealed class E2eScriptedAgentRunner : IAgentRunner
{
    public const string ScriptPrefix = "e2e-script:";

    public async Task<AgentRunResult> RunAsync(
        AgentDefinition definition,
        AgentInput input,
        IReadOnlyCollection<IAgentTool> tools,
        AgentExecutionContext context,
        AgentExecutionLimits limits,
        CancellationToken cancellationToken = default)
    {
        string message = input.UserInput ?? string.Empty;
        if (!message.StartsWith(ScriptPrefix, StringComparison.Ordinal))
            return Failure(AgentRunErrorCode.ProviderUnavailable, "The E2E host has no model provider.");

        Script? script;
        try
        {
            script = JsonSerializer.Deserialize<Script>(message[ScriptPrefix.Length..], ScriptJson);
        }
        catch (JsonException)
        {
            return Failure(AgentRunErrorCode.InvalidRequest, "The E2E script is not valid JSON.");
        }
        if (script?.Result is not { ValueKind: JsonValueKind.Object } result)
            return Failure(AgentRunErrorCode.InvalidRequest, "The E2E script needs a structured result.");

        List<AgentToolExecution> executions = [];
        foreach ((ScriptCall call, int index) in (script.Calls ?? []).Select((call, index) => (call, index)))
        {
            IAgentTool? tool = tools.FirstOrDefault(candidate =>
                string.Equals(candidate.Definition.Name, call.Tool, StringComparison.Ordinal));
            if (tool is null)
                return Failure(AgentRunErrorCode.UnknownTool, $"The E2E script called an unavailable tool '{call.Tool}'.");
            string callId = $"e2e-call-{index + 1}";
            string arguments = call.Arguments.ValueKind == JsonValueKind.Undefined ? "{}" : call.Arguments.GetRawText();
            AgentToolResult toolResult = await tool.ExecuteAsync(new AgentToolCall(callId, call.Tool, arguments), context, cancellationToken);
            executions.Add(new AgentToolExecution(call.Tool, callId, toolResult.Output, toolResult.IsError, arguments));
        }

        return new AgentRunResult(true, result.GetRawText(), result.Clone(), new AgentRunUsage(0, 0), TimeSpan.Zero, 1, executions.Count)
        {
            History = "[]",
            ToolExecutions = executions
        };
    }

    private static AgentRunResult Failure(AgentRunErrorCode code, string message) =>
        new(false, null, null, new AgentRunUsage(0, 0), TimeSpan.Zero, 0, 0, new AgentRunError(code, message));

    private static readonly JsonSerializerOptions ScriptJson = new(JsonSerializerDefaults.Web);

    private sealed record Script(IReadOnlyList<ScriptCall>? Calls, JsonElement Result);

    private sealed record ScriptCall(string Tool, JsonElement Arguments);
}
