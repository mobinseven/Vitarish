
using MediatR;
using Vorn.Aaas.AuthenticationClients.Application.Commands;
using Vorn.Aaas.AuthenticationClients.Application.Queries;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;
using Vorn.Aaas.AuthenticationClients.Presentation.Abstractions;
using Vorn.Aaas.AuthenticationClients.Presentation.Contracts;

namespace Vorn.Aaas.AuthenticationClients.Infrastructure.Services;
/// <inheritdoc />
public sealed class AuthenticationClientsService(ISender sender) : IAuthenticationClientsService
{

    public Task<AuthenticationClient?> GetAuthenticationClientAsync(string id, CancellationToken ct = default) =>
        sender.Send(new GetAuthenticationClient(id), ct);

    public Task<IEnumerable<AuthenticationClient>> GetAuthenticationClientsAsync(CancellationToken ct = default) =>
        sender.Send(new GetAuthenticationClients(), ct);

    public async Task<AuthenticationClient?> CreateAuthenticationClientAsync(CreateAuthenticationClientRequest request,
                                                              CancellationToken ct = default)
        => await sender.Send(new CreateAuthenticationClient(request.Id,
                                                             request.Url,
                                                             request.Title,
                                                             request.Description,
                                                             request.LogoUrl,
                                                             request.AutoAccess),
                             ct);

    public async Task UpdateAuthenticationClientAsync(UpdateAuthenticationClientRequest request,
                                                      CancellationToken ct = default)
        => await sender.Send(new UpdateAuthenticationClient(request.Id,
                                                             request.Url,
                                                             request.Title,
                                                             request.Description,
                                                             request.LogoUrl,
                                                             request.AutoAccess),
                             ct);

    public async Task DeleteAuthenticationClientAsync(DeleteAuthenticationClientRequest request,
                                                      CancellationToken ct = default)
        => await sender.Send(new DeleteAuthenticationClient(request.Id), ct);
}
