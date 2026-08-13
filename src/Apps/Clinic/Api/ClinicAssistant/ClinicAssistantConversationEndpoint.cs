using System.Text.Json;
using MediatR;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Contracts.ClinicAssistant;
using Vitarish.Intelligence.Application.Agents;
using ContractActionStatus = Vitarish.Clinic.Contracts.ClinicAssistant.ClinicAssistantActionStatus;

namespace Vitarish.Clinic.Api.ClinicAssistant;

/// <summary>Adapts the Intelligence-owned conversation transport to Clinic's patient workflow.</summary>
public sealed class ClinicAssistantConversationEndpoint(
    IAssistantContextFactory contextFactory,
    ISender sender) : IAgentConversationEndpoint
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    public IReadOnlyCollection<AgentConversationRoute> Routes { get; } =
    [
        new("clinic.assistant.patient", "patient"),
        new("clinic.assistant.operations", "practice"),
        new("clinic.patient", "patient"),
        new("clinic.operations", "practice")
    ];

    public async Task<AgentConversationEndpointResult> RunAsync(
        AgentConversationEndpointRequest request,
        CancellationToken cancellationToken)
    {
        bool patient = IsAgent(request.AgentKey, "clinic.assistant.patient", "clinic.patient") && string.Equals(request.SubjectKind, "patient", StringComparison.Ordinal);
        bool operations = IsAgent(request.AgentKey, "clinic.assistant.operations", "clinic.operations") && string.Equals(request.SubjectKind, "practice", StringComparison.Ordinal);
        if (!patient && !operations)
            return new(AgentConversationOutcome.NotFound);

        AssistantContext context;
        if (patient)
        {
            if (!Guid.TryParse(request.SubjectId, out Guid patientId) || patientId == Guid.Empty) return new(AgentConversationOutcome.NotFound);
            try
            {
            context = await contextFactory.CreateAsync(patientId, request.ConversationId, cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                return new(AgentConversationOutcome.NotFound);
            }
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
            // The browser has no tenant identifier contract. Treat the sentinel as a request for
            // the authenticated practice; any concrete subject id must still match that context.
            if (!string.Equals(request.SubjectId, "practice", StringComparison.Ordinal) &&
                !string.Equals(context.PracticeId, request.SubjectId, StringComparison.Ordinal))
                return new(AgentConversationOutcome.NotFound);
        }
        context = context with { AgentKey = request.AgentKey };
        AssistantResult result = await sender.Send(new RunAssistant(context, new AssistantInput(request.Message)), cancellationToken);
        if (result.Outcome is AssistantRunOutcome.NotFound)
            return new(AgentConversationOutcome.NotFound, result.AgentRunId, ParseConversationId(result.ConversationId));
        if (result.Outcome is AssistantRunOutcome.Busy)
            return new(AgentConversationOutcome.Busy, result.AgentRunId, ParseConversationId(result.ConversationId));
        if (result.Outcome is AssistantRunOutcome.ProfileChanged)
            return new(AgentConversationOutcome.ProfileChanged, result.AgentRunId, ParseConversationId(result.ConversationId));
        if (result.Outcome is AssistantRunOutcome.InvalidHistory)
            return new(AgentConversationOutcome.InvalidHistory, result.AgentRunId, ParseConversationId(result.ConversationId));
        if (result.Outcome is not AssistantRunOutcome.Succeeded || !Guid.TryParse(result.ConversationId, out Guid conversationId))
            return new(AgentConversationOutcome.ProviderFailure, result.AgentRunId, ParseConversationId(result.ConversationId));

        RunClinicAssistantResponse response = new(
            result.AgentRunId,
            result.ConversationId,
            null,
            result.Messages.Select(message => new ClinicAssistantMessageResponse(
                (ClinicAssistantMessageResponseKind)(int)message.Kind,
                message.Content)).ToArray(),
            result.Actions.Select(action => new ClinicAssistantActionResponse(
                action.ActionId.ToString("D"), action.Capability, action.ActionType,
                (ContractActionStatus)(int)action.Status,
                string.IsNullOrWhiteSpace(action.ReviewPayload)
                    ? default
                    : JsonSerializer.Deserialize<JsonElement>(action.ReviewPayload), action.Summary)).ToArray(),
            result.Candidates?.Select(candidate => new ClinicAssistantCandidateResponse(
                candidate.CandidateToken, candidate.DisplayName, candidate.DateOfBirth,
                candidate.MaskedIdentification, candidate.MaskedPhone)).ToArray(),
            result.HasMoreCandidates);
        return new(AgentConversationOutcome.Succeeded, result.AgentRunId, conversationId, JsonSerializer.SerializeToElement(response, WebJson));
    }

    private static Guid? ParseConversationId(string value) => Guid.TryParse(value, out Guid id) ? id : null;

    private static bool IsAgent(string supplied, string current, string legacy) =>
        string.Equals(supplied, current, StringComparison.Ordinal) ||
        string.Equals(supplied, legacy, StringComparison.Ordinal);
}
