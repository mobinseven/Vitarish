using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Domain.Abstractions;

public interface IVisitWorkflowService
{
    Task<VisitWorkflowReadDto> CreateAsync(VisitWorkflowWriteDto writeDto, string actorId, CancellationToken cancellationToken = default);
    Task<VisitWorkflowReadDto?> GetAsync(Guid workflowId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VisitWorkflowReadDto>> ListAsync(VisitWorkflowFilter filter, CancellationToken cancellationToken = default);
    Task<VisitWorkflowReadDto> TransitionAsync(Guid workflowId, VisitWorkflowStatus targetStatus, string actorId, string? reason = null, CancellationToken cancellationToken = default);
    Task<VisitWorkflowReadDto> CancelAsync(Guid workflowId, string actorId, string reason, CancellationToken cancellationToken = default);
    Task<VisitWorkflowReadDto> ReopenAsync(Guid workflowId, string actorId, string reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OperationalProjectionSummary>> GetQueueAsync(string projectionType, string? locationId = null, CancellationToken cancellationToken = default);
}
