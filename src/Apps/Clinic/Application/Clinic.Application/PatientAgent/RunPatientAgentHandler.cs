using System.Text.Json;
using MediatR;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent;

public sealed class RunPatientAgentHandler(
    IAgentRunner agentRunner,
    PatientWorkspaceAgentDefinitionFactory definitionFactory)
    : IRequestHandler<RunPatientAgent, PatientAgentResult>
{
    public async Task<PatientAgentResult> Handle(RunPatientAgent request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Input.Message))
        {
            return PatientAgentResult.Failure("A message is required.");
        }

        PatientWorkspaceAgentDefinition workspace = definitionFactory.Create(request.Context);
        AgentRunResult run = await agentRunner.RunAsync(
            workspace.Definition,
            new AgentInput(request.Input.Message),
            workspace.Tools,
            workspace.ExecutionContext,
            workspace.Limits,
            cancellationToken);

        if (!run.IsSuccess || run.StructuredOutput is not JsonElement output)
        {
            return PatientAgentResult.Failure(run.Error?.Message ?? "The patient agent did not return a valid result.");
        }

        try
        {
            return Parse(output);
        }
        catch (JsonException)
        {
            return PatientAgentResult.Failure("The patient agent returned an invalid structured result.");
        }
    }

    private static PatientAgentResult Parse(JsonElement output)
    {
        List<PatientAgentMessage> messages = [];
        foreach (JsonElement item in output.GetProperty("messages").EnumerateArray())
        {
            PatientAgentMessageKind kind = item.GetProperty("kind").GetString() switch
            {
                "assistantMessage" => PatientAgentMessageKind.AssistantMessage,
                "informationalToolResult" => PatientAgentMessageKind.InformationalToolResult,
                "missingInformationRequest" => PatientAgentMessageKind.MissingInformationRequest,
                _ => PatientAgentMessageKind.Failure
            };
            messages.Add(new PatientAgentMessage(kind, item.GetProperty("content").GetString() ?? string.Empty));
        }

        List<PatientAgentAction> actions = [];
        foreach (JsonElement item in output.GetProperty("actions").EnumerateArray())
        {
            actions.Add(new PatientAgentAction(
                item.GetProperty("id").GetString() ?? string.Empty,
                item.GetProperty("capability").GetString() ?? string.Empty,
                item.GetProperty("actionType").GetString() ?? string.Empty,
                PatientAgentActionStatus.Proposed,
                item.GetProperty("arguments").GetRawText(),
                item.GetProperty("summary").GetString() ?? string.Empty));
        }

        return new PatientAgentResult(messages, actions);
    }
}
