using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class GetConnectorHealthQueryHandler(IClinicalConnectorHealthService healthService)
    : IRequestHandler<GetConnectorHealthQuery, ConnectorHealthReport>
{
    public Task<ConnectorHealthReport> Handle(GetConnectorHealthQuery request, CancellationToken cancellationToken) =>
        healthService.ProbeAsync(cancellationToken: cancellationToken);
}
