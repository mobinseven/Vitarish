using MediatR;
using Vitarish.Intelligence.Application.Agents;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.ProjectManagement.Application;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.ProjectManagement.Api;

public sealed class ProjectManagementAgentConversationEndpoint(IRequestContext requestContext, IUsersService users, ISeaManagementAccessEvaluator seaAccess, ISender sender) : IAgentConversationEndpoint
{
    public IReadOnlyCollection<AgentConversationRoute> Routes { get; } = [new("project-management.event", "event")];

    public async Task<AgentConversationEndpointResult> RunAsync(AgentConversationEndpointRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.SubjectId, out Guid eventId) || eventId == Guid.Empty ||
            string.IsNullOrWhiteSpace(requestContext.TenantId) || string.IsNullOrWhiteSpace(requestContext.UserId))
        {
            return new(AgentConversationOutcome.NotFound);
        }

        if (!Guid.TryParse(requestContext.TenantId, out Guid tenantId) || tenantId == Guid.Empty)
        {
            return new(AgentConversationOutcome.NotFound);
        }

        if (request.ConversationId is { Length: > 0 } && !Guid.TryParse(request.ConversationId, out _))
        {
            return new(AgentConversationOutcome.NotFound);
        }

        bool isMember = await users.CheckUserRoleAsync(requestContext.UserId, IdentityDefinitions.PlatformAdmin, cancellationToken) ||
            await users.CheckUserRoleAsync(requestContext.UserId, $"{tenantId:D}:{IdentityDefinitions.TenantOwner}", cancellationToken) ||
            await users.CheckUserRoleAsync(requestContext.UserId, $"{tenantId:D}:{IdentityDefinitions.TenantUser}", cancellationToken);
        if (!isMember || !await seaAccess.CanReadEventAsync(new(tenantId.ToString("D"), requestContext.UserId), eventId, cancellationToken))
        {
            return new(AgentConversationOutcome.NotFound);
        }

        ProjectManagementAgentResult result = await sender.Send(new RunProjectManagementAgent(
            new ProjectManagementAgentContext(tenantId.ToString("D"), requestContext.UserId, eventId, "fa",
                Guid.TryParse(request.ConversationId, out Guid conversationId) ? conversationId : null), request.Message), cancellationToken);
        return new(result.Outcome, result.AgentRunId, result.ConversationId, result.Result);
    }
}
