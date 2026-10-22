using System.Text;
using MediatR;
using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Landing.Application;

public sealed record RunLandingConversation(string TenantId, string PublicSessionId, string OwnerKey, string Message, Guid? ConversationId) : IRequest<LandingConversationResult>;
public sealed record DeleteLandingConversation(string TenantId, string PublicSessionId, string OwnerKey, Guid ConversationId) : IRequest<AgentConversationOutcome>;
public sealed record LandingConversationResult(AgentConversationOutcome Outcome, Guid? ConversationId = null, AgentInteractiveReply? Reply = null);

public sealed class LandingConversationAgentOptions
{
    public string Model { get; set; } = string.Empty;
}

public sealed class RunLandingConversationHandler(ISender sender, IOptions<LandingConversationAgentOptions> agentOptions) : IRequestHandler<RunLandingConversation, LandingConversationResult>
{
    internal static readonly AgentInteractionVocabulary Vocabulary = new(LandingFeatureCatalog.Keys, LandingNextActions.All);
    internal static readonly string Instructions = BuildInstructions();

    public async Task<LandingConversationResult> Handle(RunLandingConversation request, CancellationToken cancellationToken)
    {
        RunAgentConversationResult result = await sender.Send(new RunAgentConversation(
            request.ConversationId,
            Scope(request.TenantId, request.PublicSessionId, request.OwnerKey),
            new AgentDefinition(agentOptions.Value.Model, Instructions, AgentInteractiveReplyFormat.Definition(Vocabulary)),
            new AgentInput(request.Message), [],
            new AgentExecutionContext(Guid.NewGuid().ToString("N")),
            new AgentExecutionLimits(MaxIterations: 2, MaxToolCalls: 0, MaxTokens: 1_500, Timeout: TimeSpan.FromSeconds(45)),
            // A reply with no usable text is refused before the turn is saved, so a retry starts clean.
            BeforeCommit: (_, run, _) => Task.FromResult(AgentInteractiveReplyFormat.Read(run, Vocabulary) is null
                ? AgentConversationOutcome.ProviderFailure
                : AgentConversationOutcome.Succeeded)), cancellationToken);
        return result.Outcome == AgentConversationOutcome.Succeeded && result.Run is { } run
            ? new(result.Outcome, result.ConversationId, AgentInteractiveReplyFormat.Read(run, Vocabulary))
            : new(result.Outcome, result.ConversationId);
    }

    internal static AgentConversationScope Scope(string tenantId, string sessionId, string ownerKey) =>
        new(tenantId, sessionId, "landing.guide", "landing-page", ownerKey);

    private static string BuildInstructions()
    {
        StringBuilder text = new("""
            You are Vitarish, speaking with a visitor on the Vitarish website. Vitarish is a platform whose purpose is to
            help people and businesses achieve theirs; today its live business application manages clinics and private
            practices. Salons, spas, e-shops and an AI dictionary are planned, not available.

            Speak like a calm, experienced colleague who has run a clinic: brief, specific, never salesy, no exclamation
            marks. Answer in the visitor's language. In Persian, use the polite شما form and say کلینیک for clinic.

            Help a clinic owner see where Vitarish fits their day. Ask about their clinic, how patients book today and
            what costs them the most time, one question at a time, offering 2 to 5 short options they can tap. Then point
            to the features that match and recommend one first thing to start with, keeping the rest of their routine.

            Only describe features from this list, and say plainly when one is in progress or not available yet:
            """);
        text.AppendLine();
        foreach (LandingFeature feature in LandingFeatureCatalog.Clinic)
            text.Append("- ").Append(feature.Key).Append(" (").Append(feature.Status).Append("): ").AppendLine(feature.Fact);
        text.Append($$"""

            Put the keys of the features you mention in "{{AgentInteractiveReplyFormat.ReferencesProperty}}". Never invent features, customers, numbers,
            testimonials or prices. Vitarish is free to start, with no time limit; do not promise that every future
            feature stays free, and do not state prices or plan limits. When unsure, say you don't know yet.

            Offer at most one next step in "{{AgentInteractiveReplyFormat.NextActionProperty}}": "{{LandingNextActions.OpenClinic}}" when they are ready to set up their clinic,
            "{{LandingNextActions.TalkToTeam}}" when they want a person (the team answers on WhatsApp), "{{LandingNextActions.TryVoice}}" when dictation would help
            them. Leave "{{AgentInteractiveReplyFormat.QuestionProperty}}" null when you are not asking anything. Do not ask for or repeat patient details.
            """);
        return text.ToString();
    }
}

public sealed class DeleteLandingConversationHandler(ISender sender) : IRequestHandler<DeleteLandingConversation, AgentConversationOutcome>
{
    public Task<AgentConversationOutcome> Handle(DeleteLandingConversation request, CancellationToken cancellationToken) =>
        sender.Send(new Vitarish.Intelligence.Application.Agents.DeleteAgentConversation(request.ConversationId, RunLandingConversationHandler.Scope(request.TenantId, request.PublicSessionId, request.OwnerKey)), cancellationToken);
}
