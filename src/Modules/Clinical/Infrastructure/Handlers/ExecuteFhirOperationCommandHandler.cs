using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class ExecuteFhirOperationCommandHandler(IClinicalOperationsService operationsService)
    : IRequestHandler<ExecuteFhirOperationCommand, FhirOperationResponse>
{
    public Task<FhirOperationResponse> Handle(ExecuteFhirOperationCommand request, CancellationToken cancellationToken) =>
        operationsService.ExecuteAsync(request.Request, cancellationToken);
}
