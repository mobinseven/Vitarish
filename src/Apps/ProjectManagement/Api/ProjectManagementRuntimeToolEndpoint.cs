using Vitarish.Intelligence.Application.Agents;
using Vitarish.Modules.Base.Core.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Sea.Application.Agents;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.ProjectManagement.Api;

public sealed class ProjectManagementRuntimeToolEndpoint(
    ISeaManagementAgentTool seaTool,
    IUsersService users,
    ISeaManagementAccessEvaluator seaAccess,
    IRequestContextWriter requestContextWriter) : IAgentRuntimeToolEndpoint
{
    public IReadOnlyCollection<string> AgentKeys { get; } = ["project-management.event"];

    public async Task<AgentToolResult> ExecuteAsync(AgentRuntimeToolEndpointRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> metadata = request.ExecutionContext.Metadata ?? throw new ArgumentException("Agent execution metadata is required.");
        if (!metadata.TryGetValue("tenantId", out string? tenantId) || !Guid.TryParse(tenantId, out Guid tenant) ||
            !metadata.TryGetValue("authenticatedUserId", out string? userId) || !Guid.TryParse(userId, out _) ||
            !metadata.TryGetValue("subjectKind", out string? subjectKind) || !string.Equals(subjectKind, "event", StringComparison.Ordinal) ||
            !metadata.TryGetValue("subjectId", out string? subjectId) || !Guid.TryParse(subjectId, out Guid eventId) || eventId == Guid.Empty ||
            !metadata.TryGetValue("agentKey", out string? agentKey) || !string.Equals(agentKey, "project-management.event", StringComparison.Ordinal))
        {
            throw new ArgumentException("Agent execution metadata is invalid.");
        }

        requestContextWriter.SetContext(
            authority: "vitarish-agent-runtime",
            clientId: "agent-runtime",
            ownerId: userId,
            tenantId: tenant.ToString("D"),
            userId: userId,
            resourceId: eventId.ToString("D"),
            traceId: request.ExecutionContext.ExecutionId,
            requestId: request.ToolCall.Id,
            additionalInfo: "Signed project-management event-agent tool callback.");

        SeaManagementAgentSubject subject = new(tenant.ToString("D"), userId);
        bool member = await users.CheckUserRoleAsync(userId, IdentityDefinitions.PlatformAdmin, cancellationToken) ||
            await users.CheckUserRoleAsync(userId, $"{tenant:D}:{IdentityDefinitions.TenantOwner}", cancellationToken) ||
            await users.CheckUserRoleAsync(userId, $"{tenant:D}:{IdentityDefinitions.TenantUser}", cancellationToken);
        if (!member || !await seaAccess.CanReadEventAsync(subject, eventId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Current event access is required.");
        }

        if (!string.Equals(request.ToolCall.Name, seaTool.Definition.Name, StringComparison.Ordinal))
        {
            throw new KeyNotFoundException();
        }

        return await seaTool.ExecuteAsync(request.ToolCall, request.ExecutionContext, cancellationToken);
    }
}
