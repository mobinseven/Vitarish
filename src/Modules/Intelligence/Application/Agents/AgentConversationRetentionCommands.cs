using MediatR;

namespace Vitarish.Intelligence.Application.Agents;

public sealed record DeleteAgentConversationsForOwner(AgentConversationOwnerScope Owner)
    : IRequest<AgentConversationOwnerDeletionResult>;

public sealed class DeleteAgentConversationsForOwnerHandler(IAgentConversationStore store)
    : IRequestHandler<DeleteAgentConversationsForOwner, AgentConversationOwnerDeletionResult>
{
    public Task<AgentConversationOwnerDeletionResult> Handle(DeleteAgentConversationsForOwner request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Owner.TenantId) &&
            string.IsNullOrWhiteSpace(request.Owner.CreatingUserId) &&
            string.IsNullOrWhiteSpace(request.Owner.SubjectKind) &&
            string.IsNullOrWhiteSpace(request.Owner.SubjectId))
        {
            throw new ArgumentException("At least one owner scope value is required.", nameof(request));
        }

        return store.DeleteForOwnerAsync(request.Owner, cancellationToken);
    }
}
