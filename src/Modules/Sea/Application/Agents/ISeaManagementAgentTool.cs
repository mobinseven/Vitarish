using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Sea.Application.Agents;

/// <summary>SEA-owned agent capability. Mutations are prepared first and may only be executed from a typed prepared action.</summary>
public interface ISeaManagementAgentTool : IAgentTool
{
    SeaManagementPreparedAction? ParsePreparedAction(AgentToolResult result);

    Task<AgentToolResult> ExecutePreparedAsync(
        AgentToolCall call,
        SeaManagementPreparedAction action,
        SeaManagementAgentSubject subject,
        CancellationToken cancellationToken = default);
}

public sealed record SeaManagementPreparedAction(
    Guid ActionId,
    string Operation,
    string PayloadJson,
    DateTimeOffset ExpiresAt);

public sealed record SeaManagementAgentSubject(string TenantId, string UserId, Guid? EventId = null);

/// <summary>Authoritative SEA authorization boundary for agent operations.</summary>
public interface ISeaManagementAccessEvaluator
{
    Task<bool> CanAccessTenantWorkspaceAsync(SeaManagementAgentSubject subject, CancellationToken cancellationToken = default);
    Task PrimeEventRolesAsync(IEnumerable<Guid> eventIds, CancellationToken cancellationToken = default);
    Task<bool> IsAuthorizedAsync(
        SeaManagementAgentSubject subject,
        string operation,
        string payloadJson,
        CancellationToken cancellationToken = default);
    Task<bool> CanReadProjectAsync(SeaManagementAgentSubject subject, Guid projectId, CancellationToken cancellationToken = default);
    Task<bool> CanReadProcedureAsync(SeaManagementAgentSubject subject, Guid procedureId, CancellationToken cancellationToken = default);
    Task<bool> CanReadEventAsync(SeaManagementAgentSubject subject, Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> CanWriteEventAsync(SeaManagementAgentSubject subject, Guid eventId, CancellationToken cancellationToken = default);
}
