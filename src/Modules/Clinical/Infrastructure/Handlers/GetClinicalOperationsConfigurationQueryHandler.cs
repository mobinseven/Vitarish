using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class GetClinicalOperationsConfigurationQueryHandler(IClinicalOperationsService operationsService)
    : IRequestHandler<GetClinicalOperationsConfigurationQuery, ClinicalOperationsConfiguration>
{
    public Task<ClinicalOperationsConfiguration> Handle(GetClinicalOperationsConfigurationQuery request, CancellationToken cancellationToken) =>
        operationsService.GetConfigurationAsync(cancellationToken);
}
