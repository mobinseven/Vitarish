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
            return Parse(output, workspace.EnabledActions);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return PatientAgentResult.Failure("The patient agent returned an invalid structured result.");
        }
    }

    private static PatientAgentResult Parse(
        JsonElement output,
        IReadOnlyDictionary<string, IReadOnlySet<string>> enabledActions)
    {
        List<PatientAgentMessage> messages = [];
        foreach (JsonElement item in output.GetProperty("messages").EnumerateArray())
        {
            PatientAgentMessageKind kind = item.GetProperty("kind").GetString() switch
            {
                "assistantMessage" => PatientAgentMessageKind.AssistantMessage,
                "informationalToolResult" => PatientAgentMessageKind.InformationalToolResult,
                "missingInformationRequest" => PatientAgentMessageKind.MissingInformationRequest,
                "failure" => PatientAgentMessageKind.Failure,
                _ => throw new JsonException("Unknown patient agent message kind.")
            };
            messages.Add(new PatientAgentMessage(kind, item.GetProperty("content").GetString() ?? string.Empty));
        }

        List<PatientAgentAction> actions = [];
        foreach (JsonElement item in output.GetProperty("actions").EnumerateArray())
        {
            string capability = item.GetProperty("capability").GetString() ?? string.Empty;
            string actionType = item.GetProperty("actionType").GetString() ?? string.Empty;
            if (!enabledActions.TryGetValue(capability, out IReadOnlySet<string>? actionTypes) || !actionTypes.Contains(actionType))
            {
                throw new JsonException("The patient agent returned an action that is not enabled.");
            }

            JsonElement arguments = item.GetProperty("arguments");
            if (item.GetProperty("status").GetString() != "proposed" ||
                arguments.ValueKind != JsonValueKind.Object ||
                ContainsPatientId(arguments))
            {
                throw new JsonException("Patient action arguments cannot select a patient.");
            }

            actions.Add(new PatientAgentAction(
                item.GetProperty("id").GetString() ?? string.Empty,
                capability,
                actionType,
                PatientAgentActionStatus.Proposed,
                arguments.GetRawText(),
                item.GetProperty("summary").GetString() ?? string.Empty));
        }

        return new PatientAgentResult(messages, actions);
    }

    private static bool ContainsPatientId(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            return element.EnumerateObject().Any(property =>
                string.Equals(property.Name, "patientId", StringComparison.OrdinalIgnoreCase) ||
                ContainsPatientId(property.Value));
        }

        return element.ValueKind == JsonValueKind.Array && element.EnumerateArray().Any(ContainsPatientId);
    }
}
