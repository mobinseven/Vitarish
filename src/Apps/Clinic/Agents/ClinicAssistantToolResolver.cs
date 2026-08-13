using Vitarish.Clinic.Application.Assistant;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Agents;

public sealed class ClinicAssistantToolResolver(ClinicAssistantManifestComposer manifestComposer)
{
    public async Task<AssistantToolResult> ResolveAsync(
        AssistantToolResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.ExecutionEnvelope.ExecutionId, request.ExecutionId, StringComparison.Ordinal) ||
            !string.Equals(request.ExecutionEnvelope.ToolCallId, request.ToolCallId, StringComparison.Ordinal) ||
            !string.Equals(request.ExecutionEnvelope.ToolName, request.ToolName, StringComparison.Ordinal))
            throw new InvalidOperationException("The callback tool identity does not match the signed execution envelope.");

        AgentExecutionContext execution = new(request.ExecutionId, request.ExecutionMetadata)
        {
            Envelope = request.ExecutionEnvelope
        };
        ClinicAssistantManifest manifest = manifestComposer.Compose(request.Context, execution);
        IAgentTool? tool = manifest.Tools.FirstOrDefault(candidate =>
            string.Equals(candidate.Definition.Name, request.ToolName, StringComparison.Ordinal));
        if (tool is null)
            throw new KeyNotFoundException();

        AgentToolResult result = await tool.ExecuteAsync(
            new AgentToolCall(request.ToolCallId, request.ToolName, request.ArgumentsJson),
            execution,
            cancellationToken);
        return new(result.CallId, result.Output, result.IsError);
    }
}
