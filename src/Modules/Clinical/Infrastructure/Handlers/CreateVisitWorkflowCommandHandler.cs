using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class CreateVisitWorkflowCommandHandler(IVisitWorkflowService workflowService)
    : IRequestHandler<CreateVisitWorkflowCommand, VisitWorkflowReadDto>
{
    public Task<VisitWorkflowReadDto> Handle(CreateVisitWorkflowCommand request, CancellationToken cancellationToken) =>
        workflowService.CreateAsync(request.WriteDto, request.ActorId, cancellationToken);
}
