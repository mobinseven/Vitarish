using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class ListFhirOperationsQueryHandler(IClinicalOperationsService operationsService)
    : IRequestHandler<ListFhirOperationsQuery, IReadOnlyList<ClinicalFhirOperationRecord>>
{
    public Task<IReadOnlyList<ClinicalFhirOperationRecord>> Handle(ListFhirOperationsQuery request, CancellationToken cancellationToken) =>
        operationsService.ListAsync(request.Filter, cancellationToken);
}
