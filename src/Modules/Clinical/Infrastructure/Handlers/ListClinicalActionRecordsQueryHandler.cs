using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class ListClinicalActionRecordsQueryHandler(IClinicalActionService actionService)
    : IRequestHandler<ListClinicalActionRecordsQuery, IReadOnlyList<ClinicalActionRecordReadDto>>
{
    public Task<IReadOnlyList<ClinicalActionRecordReadDto>> Handle(ListClinicalActionRecordsQuery request, CancellationToken cancellationToken) =>
        actionService.ListRecordsAsync(request.Filter, cancellationToken);
}
