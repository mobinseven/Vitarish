using System.Text.Json;
using MediatR;
using Vitarish.Clinic.Application.PatientAgent;
using Vitarish.Clinic.Contracts.PatientAgent;
using Vitarish.Intelligence.Application.Agents;
using ContractActionStatus = Vitarish.Clinic.Contracts.PatientAgent.PatientAgentActionStatus;

namespace Vitarish.Clinic.Api.PatientAgent;

/// <summary>Adapts the Intelligence-owned conversation transport to Clinic's patient workflow.</summary>
public sealed class ClinicAgentConversationEndpoint(
    IPatientAgentContextFactory contextFactory,
    ISender sender) : IAgentConversationEndpoint
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    public async Task<AgentConversationEndpointResult> RunAsync(
        AgentConversationEndpointRequest request,
        CancellationToken cancellationToken)
    {
        bool patient = string.Equals(request.AgentKey, "clinic.patient", StringComparison.Ordinal) && string.Equals(request.SubjectKind, "patient", StringComparison.Ordinal);
        bool operations = string.Equals(request.AgentKey, "clinic.operations", StringComparison.Ordinal) && string.Equals(request.SubjectKind, "practice", StringComparison.Ordinal);
        if (!patient && !operations)
            return new(AgentConversationOutcome.NotFound);

        PatientAgentContext context;
        if (patient)
        {
            if (!Guid.TryParse(request.SubjectId, out Guid patientId) || patientId == Guid.Empty) return new(AgentConversationOutcome.NotFound);
            context = await contextFactory.CreateAsync(patientId, request.ConversationId, cancellationToken);
        }
        else
        {
            try
            {
                context = await contextFactory.CreateOperationsAsync(request.ConversationId, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                return new(AgentConversationOutcome.NotFound);
            }
            if (!string.Equals(context.PracticeId, request.SubjectId, StringComparison.Ordinal)) return new(AgentConversationOutcome.NotFound);
        }
        PatientAgentResult result = await sender.Send(new RunPatientAgent(context, new PatientAgentInput(request.Message)), cancellationToken);
        if (result.Outcome is PatientAgentRunOutcome.NotFound)
            return new(AgentConversationOutcome.NotFound, result.AgentRunId, ParseConversationId(result.ConversationId));
        if (result.Outcome is PatientAgentRunOutcome.Busy)
            return new(AgentConversationOutcome.Busy, result.AgentRunId, ParseConversationId(result.ConversationId));
        if (result.Outcome is not PatientAgentRunOutcome.Succeeded || !Guid.TryParse(result.ConversationId, out Guid conversationId))
            return new(AgentConversationOutcome.ProviderFailure, result.AgentRunId, ParseConversationId(result.ConversationId));

        RunPatientAgentResponse response = new(
            result.AgentRunId,
            result.ConversationId,
            null,
            result.Messages.Select(message => new PatientAgentMessageResponse(
                (PatientAgentMessageResponseKind)(int)message.Kind,
                message.Content)).ToArray(),
            result.Actions.Select(action => new PatientAgentActionResponse(
                action.ActionId.ToString("D"), action.Capability, action.ActionType,
                (ContractActionStatus)(int)action.Status,
                string.IsNullOrWhiteSpace(action.ReviewPayload)
                    ? default
                    : JsonSerializer.Deserialize<JsonElement>(action.ReviewPayload), action.Summary)).ToArray());
        return new(AgentConversationOutcome.Succeeded, result.AgentRunId, conversationId, JsonSerializer.SerializeToElement(response, WebJson));
    }

    private static Guid? ParseConversationId(string value) => Guid.TryParse(value, out Guid id) ? id : null;
}
