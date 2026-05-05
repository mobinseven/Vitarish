using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class SyncProjectionCommandHandler(IClinicalResourceProjectionService projectionService)
    : IRequestHandler<SyncProjectionCommand, ProjectionReadDto>
{
    public Task<ProjectionReadDto> Handle(SyncProjectionCommand request, CancellationToken cancellationToken) =>
        projectionService.SyncAsync(request.ResourceType, request.ResourceId, request.ResourceLinkId, cancellationToken);
}
