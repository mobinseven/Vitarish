using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class GetResourceLinkQueryHandler(IClinicalResourceLinkService linkService)
    : IRequestHandler<GetResourceLinkQuery, ResourceLinkReadDto?>
{
    public Task<ResourceLinkReadDto?> Handle(GetResourceLinkQuery request, CancellationToken cancellationToken) =>
        linkService.GetLinkAsync(request.Id, cancellationToken);
}
