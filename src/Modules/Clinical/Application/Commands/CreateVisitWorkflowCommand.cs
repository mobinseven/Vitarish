using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record CreateVisitWorkflowCommand(
    VisitWorkflowWriteDto WriteDto,
    string ActorId) : IRequest<VisitWorkflowReadDto>;
