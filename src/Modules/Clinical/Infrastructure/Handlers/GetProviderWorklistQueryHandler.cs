using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class GetProviderWorklistQueryHandler(IProjectionBuilder projectionBuilder)
    : IRequestHandler<GetProviderWorklistQuery, IReadOnlyList<OperationalProjectionSummary>>
{
    public Task<IReadOnlyList<OperationalProjectionSummary>> Handle(GetProviderWorklistQuery request, CancellationToken cancellationToken) =>
        projectionBuilder.GetQueueAsync("provider", null, cancellationToken);
}
