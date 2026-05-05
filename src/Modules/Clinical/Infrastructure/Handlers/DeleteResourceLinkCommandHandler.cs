using MediatR;
using Vitarish.Clinical.Application.Commands;
using Vitarish.Clinical.Domain.Abstractions;

namespace Vitarish.Clinical.Infrastructure.Handlers;

internal sealed class DeleteResourceLinkCommandHandler(IClinicalResourceLinkService linkService)
    : IRequestHandler<DeleteResourceLinkCommand>
{
    public Task Handle(DeleteResourceLinkCommand request, CancellationToken cancellationToken) =>
        linkService.UnlinkAsync(request.Id, cancellationToken);
}
