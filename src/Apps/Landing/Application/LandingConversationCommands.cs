using MediatR;
using Microsoft.Extensions.Options;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Landing.Application;

public sealed record RunLandingConversation(string TenantId, string PublicSessionId, string OwnerKey, string Message, Guid? ConversationId) : IRequest<LandingConversationResult>;
public sealed record DeleteLandingConversation(string TenantId, string PublicSessionId, string OwnerKey, Guid ConversationId) : IRequest<AgentConversationOutcome>;
public sealed record LandingConversationResult(AgentConversationOutcome Outcome, Guid? ConversationId = null, string? Message = null);

public sealed class LandingConversationAgentOptions
{
    public string Model { get; set; } = string.Empty;
}

public sealed class RunLandingConversationHandler(ISender sender, IOptions<LandingConversationAgentOptions> agentOptions) : IRequestHandler<RunLandingConversation, LandingConversationResult>
{
    public async Task<LandingConversationResult> Handle(RunLandingConversation request, CancellationToken cancellationToken)
    {
        RunAgentConversationResult result = await sender.Send(new RunAgentConversation(
            request.ConversationId,
            Scope(request.TenantId, request.PublicSessionId, request.OwnerKey),
            new AgentDefinition(agentOptions.Value.Model, "You are Vitarish, a concise, thoughtful guide. Help the visitor clarify decisions and identify one practical next step. Answer in the language used by the visitor. Do not claim access to private data or professional authority."),
            new AgentInput(request.Message), [],
            new AgentExecutionContext(Guid.NewGuid().ToString("N")),
            new AgentExecutionLimits(MaxIterations: 2, MaxToolCalls: 0, MaxTokens: 1_024, Timeout: TimeSpan.FromSeconds(45))), cancellationToken);
        return new(result.Outcome, result.ConversationId, result.Run?.Output);
    }

    internal static AgentConversationScope Scope(string tenantId, string sessionId, string ownerKey) =>
        new(tenantId, sessionId, "landing.guide", "landing-page", ownerKey);
}

public sealed class DeleteLandingConversationHandler(ISender sender) : IRequestHandler<DeleteLandingConversation, AgentConversationOutcome>
{
    public Task<AgentConversationOutcome> Handle(DeleteLandingConversation request, CancellationToken cancellationToken) =>
        sender.Send(new Vitarish.Intelligence.Application.Agents.DeleteAgentConversation(request.ConversationId, RunLandingConversationHandler.Scope(request.TenantId, request.PublicSessionId, request.OwnerKey)), cancellationToken);
}
