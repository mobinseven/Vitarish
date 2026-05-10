using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class GetVisitQueueQueryHandler(IProjectionBuilder projectionBuilder)
    : IRequestHandler<GetVisitQueueQuery, IReadOnlyList<OperationalProjectionSummary>>
{
    public Task<IReadOnlyList<OperationalProjectionSummary>> Handle(GetVisitQueueQuery request, CancellationToken cancellationToken)
    {
        string projectionType = request.Role.ToLowerInvariant() switch
        {
            "nurse" => "nurse",
            "receptionist" => "nurse",
            "clinician" or "provider" or "physician" => "provider",
            _ => "nurse"
        };

        return projectionBuilder.GetQueueAsync(projectionType, request.LocationId, cancellationToken);
    }
}
