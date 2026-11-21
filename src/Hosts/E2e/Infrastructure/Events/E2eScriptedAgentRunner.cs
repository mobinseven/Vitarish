using System.Text.Json;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.E2e.Infrastructure.Events;

/// <summary>
/// Stands in for the model provider in the E2E host. A scenario sends a user message that starts with
/// <see cref="ScriptPrefix"/> followed by a JSON script: the tool calls the "model" makes and its structured result.
/// The calls run through the same guarded tools a real run would call back into, so tool authorization, result
/// parsing and prepared-action persistence are exercised for real. Any other message fails like an unreachable
/// provider, as the E2E host has no model.
/// A script may also script the provider interval of a streamed turn: <c>steps</c> run in order before the result,
/// each either reporting one provisional answer segment (honored only when the turn declared an answer stream and
/// runs no tools, as the real runtime does) or holding the provider silent until a named gate is released.
/// <c>track</c> counts the run's start, completion and cancellation; <c>release</c> opens gates; <c>report</c>
/// answers with those counters. Gates and tracks belong to the scenario that names them.
/// </summary>
internal sealed class E2eScriptedAgentRunner(E2eScriptedTurnState state) : IAgentRunner
{
    public const string ScriptPrefix = "e2e-script:";

    public Task<AgentRunResult> RunAsync(
        AgentDefinition definition,
        AgentInput input,
        IReadOnlyCollection<IAgentTool> tools,
        AgentExecutionContext context,
        AgentExecutionLimits limits,
        CancellationToken cancellationToken = default) =>
        RunScriptAsync(input, tools, context, null, null, cancellationToken);

    public Task<AgentRunResult> RunStreamedAsync(
        AgentDefinition definition,
        AgentInput input,
        IReadOnlyCollection<IAgentTool> tools,
        AgentExecutionContext context,
        AgentExecutionLimits limits,
        AgentAnswerStream? answerStream,
        IAgentRunProgress progress,
        CancellationToken cancellationToken = default) =>
        RunScriptAsync(input, tools, context, tools.Count == 0 ? answerStream : null, progress, cancellationToken);

    private async Task<AgentRunResult> RunScriptAsync(
        AgentInput input,
        IReadOnlyCollection<IAgentTool> tools,
        AgentExecutionContext context,
        AgentAnswerStream? answerStream,
        IAgentRunProgress? progress,
        CancellationToken cancellationToken)
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
        foreach (string gate in script?.Release ?? [])
            state.Release(gate);
        JsonElement? scriptedResult = script?.Report == true
            ? ReportResult(state.Snapshot())
            : script?.Result is { ValueKind: JsonValueKind.Object } declared ? declared : null;
        if (script is null || scriptedResult is not { } result)
            return Failure(AgentRunErrorCode.InvalidRequest, "The E2E script needs a structured result.");

        string? track = script.Track;
        if (track is not null)
            state.Started(track);
        try
        {
            if (progress is not null)
                await progress.StageAsync(new(AgentTurnStage.ModelRoundStarted, 1), cancellationToken);
            foreach (ScriptStep step in script.Steps ?? [])
            {
                if (step.AnswerDelta is { } text && answerStream is not null && progress is not null)
                    await progress.AnswerDeltaAsync(answerStream.MessageId, text, cancellationToken);
                if (step.Hold is { } gate)
                    await state.WaitForReleaseAsync(gate, cancellationToken);
            }

            List<AgentToolExecution> executions = [];
            foreach ((ScriptCall call, int index) in (script.Calls ?? []).Select((call, index) => (call, index)))
            {
                IAgentTool? tool = tools.FirstOrDefault(candidate =>
                    string.Equals(candidate.Definition.Name, call.Tool, StringComparison.Ordinal));
                if (tool is null)
                    return Failure(AgentRunErrorCode.UnknownTool, $"The E2E script called an unavailable tool '{call.Tool}'.");
                string callId = $"e2e-call-{index + 1}";
                string arguments = call.Arguments.ValueKind == JsonValueKind.Undefined ? "{}" : call.Arguments.GetRawText();
                if (progress is not null)
                    await progress.StageAsync(new(AgentTurnStage.ToolStarted, index + 1), cancellationToken);
                AgentToolResult toolResult = await tool.ExecuteAsync(new AgentToolCall(callId, call.Tool, arguments), context, cancellationToken);
                if (progress is not null)
                    await progress.StageAsync(new(AgentTurnStage.ToolCompleted, index + 1), cancellationToken);
                executions.Add(new AgentToolExecution(call.Tool, callId, toolResult.Output, toolResult.IsError, arguments));
            }

            if (track is not null)
                state.Completed(track);
            return new AgentRunResult(true, result.GetRawText(), result.Clone(), new AgentRunUsage(0, 0), TimeSpan.Zero, 1, executions.Count)
            {
                History = "[]",
                ToolExecutions = executions
            };
        }
        catch (OperationCanceledException)
        {
            if (track is not null)
                state.Canceled(track);
            throw;
        }
    }

    private static JsonElement ReportResult(IReadOnlyDictionary<string, TrackSnapshot> snapshot) =>
        JsonSerializer.SerializeToElement(new
        {
            messages = new[] { new { kind = "assistantMessage", content = JsonSerializer.Serialize(snapshot, ScriptJson) } },
            actions = Array.Empty<object>()
        }, ScriptJson);

    private static AgentRunResult Failure(AgentRunErrorCode code, string message) =>
        new(false, null, null, new AgentRunUsage(0, 0), TimeSpan.Zero, 0, 0, new AgentRunError(code, message));

    private static readonly JsonSerializerOptions ScriptJson = new(JsonSerializerDefaults.Web);

    private sealed record Script(
        IReadOnlyList<ScriptCall>? Calls,
        JsonElement Result,
        IReadOnlyList<ScriptStep>? Steps = null,
        string? Track = null,
        IReadOnlyList<string>? Release = null,
        bool Report = false);

    private sealed record ScriptCall(string Tool, JsonElement Arguments);

    private sealed record ScriptStep(string? AnswerDelta = null, string? Hold = null);
}
