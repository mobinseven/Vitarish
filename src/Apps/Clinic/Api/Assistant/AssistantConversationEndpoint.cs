using System.Text.Json;
using MediatR;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Clinic.Application.Assistant.Operations;
using Vitarish.Clinic.Contracts.ClinicAssistant;
using Vitarish.Intelligence.Application.Agents;
using ContractActionStatus = Vitarish.Clinic.Contracts.ClinicAssistant.ClinicAssistantActionStatus;

namespace Vitarish.Clinic.Api.Assistant;

/// <summary>Adapts the Intelligence-owned conversation transport to Clinic's patient workflow.</summary>
public sealed class AssistantConversationEndpoint(
    IAssistantContextFactory contextFactory,
    ISender sender) : IAgentConversationEndpoint
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    // The canonical and legacy practice operations routes share the rollout decision with operations help.
    public IReadOnlyCollection<AgentConversationRoute> Routes { get; } =
    [
        new(AssistantSubjects.PatientAgentKey, AssistantSubjects.PatientKind),
        new(AssistantSubjects.OperationsAgentKey, AssistantSubjects.PracticeKind, StreamingEnabled: true),
        new(AssistantSubjects.OperationsHelpAgentKey, AssistantSubjects.PracticeKind, StreamingEnabled: true),
        new(AssistantSubjects.LegacyPatientAgentKey, AssistantSubjects.PatientKind),
        new(AssistantSubjects.LegacyOperationsAgentKey, AssistantSubjects.PracticeKind, StreamingEnabled: true)
    ];

    public async Task<AgentConversationEndpointResult> RunAsync(
        AgentConversationEndpointRequest request,
        CancellationToken cancellationToken)
    {
        bool patient = IsAgent(request.AgentKey, AssistantSubjects.PatientAgentKeys) && string.Equals(request.SubjectKind, AssistantSubjects.PatientKind, StringComparison.Ordinal);
        bool help = string.Equals(request.AgentKey, AssistantSubjects.OperationsHelpAgentKey, StringComparison.Ordinal) &&
            string.Equals(request.SubjectKind, AssistantSubjects.PracticeKind, StringComparison.Ordinal);
        bool operations = help || IsAgent(request.AgentKey, AssistantSubjects.OperationsAgentKeys) && string.Equals(request.SubjectKind, AssistantSubjects.PracticeKind, StringComparison.Ordinal);
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
        // The help entry is for the same practice staff as the assistant.
        if (help && !AssistantOperationsScope.IsAuthorizedRole(context.Role))
            return new(AgentConversationOutcome.NotFound);
        context = context with { AgentKey = request.AgentKey };
        AssistantResult result = await sender.Send(
            new RunAssistant(context, new AssistantInput(request.Message), request.Observer),
            cancellationToken);
        if (result.Outcome is AssistantRunOutcome.NotFound)
            return new(AgentConversationOutcome.NotFound, result.AgentRunId, ParseConversationId(result.ConversationId));
        if (result.Outcome is AssistantRunOutcome.Busy)
            return new(AgentConversationOutcome.Busy, result.AgentRunId, ParseConversationId(result.ConversationId));
        if (result.Outcome is AssistantRunOutcome.InvalidHistory)
            return new(AgentConversationOutcome.InvalidHistory, result.AgentRunId, ParseConversationId(result.ConversationId));
        if (result.Outcome is AssistantRunOutcome.TimedOut)
            return new(AgentConversationOutcome.TimedOut, result.AgentRunId, ParseConversationId(result.ConversationId));
        if (result.Outcome is AssistantRunOutcome.Cancelled)
            return new(AgentConversationOutcome.Cancelled, result.AgentRunId, ParseConversationId(result.ConversationId));
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
        return new(
            AgentConversationOutcome.Succeeded,
            result.AgentRunId,
            conversationId,
            JsonSerializer.SerializeToElement(response, WebJson),
            AssistantGuidedSurface.Blocks(result));
    }

    private static Guid? ParseConversationId(string value) => Guid.TryParse(value, out Guid id) ? id : null;

    private static bool IsAgent(string supplied, IEnumerable<string> keys) =>
        keys.Any(key => string.Equals(supplied, key, StringComparison.Ordinal));
}
