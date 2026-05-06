using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Queries;

public sealed record GetWorkflowStateHistoryQuery(Guid WorkflowId) : IRequest<IReadOnlyList<WorkflowStateHistoryReadDto>>;
