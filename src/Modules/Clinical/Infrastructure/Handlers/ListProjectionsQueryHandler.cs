using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class ListProjectionsQueryHandler(IClinicalResourceProjectionService projectionService)
    : IRequestHandler<ListProjectionsQuery, IReadOnlyList<ProjectionReadDto>>
{
    public Task<IReadOnlyList<ProjectionReadDto>> Handle(ListProjectionsQuery request, CancellationToken cancellationToken) =>
        projectionService.ListAsync(request.Filter, cancellationToken);
}
