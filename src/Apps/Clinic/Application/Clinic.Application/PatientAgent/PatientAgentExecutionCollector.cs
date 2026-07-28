using System.Text.Json;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.PatientAgent;

public sealed record PatientAgentPreparedAction(
    string Capability,
    string ActionType,
    Guid OwningId,
    string ReviewDataJson,
    DateTimeOffset? Expiration);

public sealed record PatientAgentToolExecution(
    string ToolName,
    string ToolCallId,
    string AgentRunId,
    PatientAgentPreparedAction PreparedAction);

public sealed class PatientAgentExecutionCollector
{
    private readonly List<PatientAgentToolExecution> _executions = [];
    private readonly object _sync = new();

    public IReadOnlyList<PatientAgentToolExecution> Executions
    {
        get { lock (_sync) return [.. _executions]; }
    }

    public void Record(string capability, AgentToolCall call, AgentExecutionContext context, AgentToolResult result)
    {
        if (result.IsError) return;
        PatientAgentPreparedAction? prepared = TryCreatePreparedAction(capability, call.Name, result.Output);
        if (prepared is null) return;

        lock (_sync)
        {
            _executions.Add(new(call.Name, call.Id, context.ExecutionId, prepared));
        }
    }

    private static PatientAgentPreparedAction? TryCreatePreparedAction(string capability, string toolName, string outputJson)
    {
        string actionType;
        string ownerProperty;
        string reviewProperty;
        string? expirationProperty;
        string preparedStatus;
        switch (toolName)
        {
            case "prepare_appointment":
                actionType = "create_appointment";
                ownerProperty = "id";
                reviewProperty = "proposal";
                expirationProperty = "expiresAt";
                preparedStatus = "prepared";
                break;
            case "prepare_prescription":
                actionType = "create_prescription";
                ownerProperty = "reviewId";
                reviewProperty = "review";
                expirationProperty = null;
                preparedStatus = "prepared";
                break;
            case "prepare_clinical_event":
                actionType = "add_clinical_event";
                ownerProperty = "reviewId";
                reviewProperty = "review";
                expirationProperty = "expiresAt";
                preparedStatus = "prepared";
                break;
            default:
                return null;
        }

        using JsonDocument document = JsonDocument.Parse(outputJson);
        JsonElement root = document.RootElement;
        if (!root.TryGetProperty("status", out JsonElement status) ||
            !string.Equals(status.GetString(), preparedStatus, StringComparison.OrdinalIgnoreCase) ||
            !root.TryGetProperty(reviewProperty, out JsonElement review) || review.ValueKind != JsonValueKind.Object ||
            !review.TryGetProperty(ownerProperty, out JsonElement owner) || !owner.TryGetGuid(out Guid owningId))
        {
            return null;
        }

        DateTimeOffset? expiration = expirationProperty is not null &&
            review.TryGetProperty(expirationProperty, out JsonElement expires) && expires.TryGetDateTimeOffset(out DateTimeOffset value)
                ? value : null;
        return new(capability, actionType, owningId, review.GetRawText(), expiration);
    }
}
