using MediatR;
using Vitarish.Clinical.Application.Queries;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class GetResourceLinksQueryHandler(IClinicalResourceLinkService linkService)
    : IRequestHandler<GetResourceLinksQuery, IReadOnlyList<ResourceLinkReadDto>>
{
    public Task<IReadOnlyList<ResourceLinkReadDto>> Handle(GetResourceLinksQuery request, CancellationToken cancellationToken) =>
        linkService.GetLinksAsync(request.Filter, cancellationToken);
}
