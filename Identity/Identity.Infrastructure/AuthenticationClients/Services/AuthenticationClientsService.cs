using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Contracts.AuthenticationClients;
using Vitarish.Identity.Core.AuthenticationClients.Application.Commands;
using Vitarish.Identity.Core.AuthenticationClients.Application.Queries;
using Vitarish.Identity.Core.AuthenticationClients.Domain.Entities;

namespace Vitarish.Identity.Infrastructure.AuthenticationClients.Services;

/// <inheritdoc />
public sealed class AuthenticationClientsService(ISender sender) : IAuthenticationClientsService
{
    public Task<AuthenticationClient?> GetAuthenticationClientAsync(string id, CancellationToken ct = default) =>
        sender.Send(new GetAuthenticationClient(id), ct);

    public Task<IEnumerable<AuthenticationClient>> GetAuthenticationClientsAsync(CancellationToken ct = default) =>
        sender.Send(new GetAuthenticationClients(), ct);

    public Task<Result<AuthenticationClient>> CreateAuthenticationClientAsync(CreateAuthenticationClientRequest request,
                                                              CancellationToken ct = default)
        => sender.Send(new CreateAuthenticationClient(request.Id,
                                                             request.Url,
                                                             request.Title,
                                                             request.Description,
                                                             request.LogoUrl,
                                                             request.AutoAccess),
                             ct);

    public Task<Result> UpdateAuthenticationClientAsync(UpdateAuthenticationClientRequest request,
                                                      CancellationToken ct = default)
        => sender.Send(new UpdateAuthenticationClient(request.Id,
                                                             request.Url,
                                                             request.Title,
                                                             request.Description,
                                                             request.LogoUrl,
                                                             request.AutoAccess),
                             ct);

    public Task<Result> DeleteAuthenticationClientAsync(DeleteAuthenticationClientRequest request,
                                                      CancellationToken ct = default)
        => sender.Send(new DeleteAuthenticationClient(request.Id), ct);
}
