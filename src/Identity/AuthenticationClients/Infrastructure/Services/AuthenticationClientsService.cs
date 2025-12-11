
using MediatR;
using Vitarish.AuthenticationClients.Application.Abstractions;
using Vitarish.AuthenticationClients.Application.Commands;
using Vitarish.AuthenticationClients.Application.Queries;
using Vitarish.AuthenticationClients.Domain.Entities;

namespace Vitarish.AuthenticationClients.Infrastructure.Services;
/// <inheritdoc />
public sealed class AuthenticationClientsService(ISender sender) : IAuthenticationClientsService
{

    public Task<AuthenticationClient?> GetAuthenticationClientAsync(string id, CancellationToken ct = default) =>
        sender.Send(new GetAuthenticationClient(id), ct);

    public Task<IEnumerable<AuthenticationClient>> GetAuthenticationClientsAsync(CancellationToken ct = default) =>
        sender.Send(new GetAuthenticationClients(), ct);

    public async Task<AuthenticationClient?> CreateAuthenticationClientAsync(CreateAuthenticationClient request,
                                                              CancellationToken ct = default)
        => await sender.Send(request,
                             ct);

    public async Task UpdateAuthenticationClientAsync(UpdateAuthenticationClient request,
                                                      CancellationToken ct = default)
        => await sender.Send(request,
                             ct);

    public async Task DeleteAuthenticationClientAsync(DeleteAuthenticationClient request,
                                                      CancellationToken ct = default)
        => await sender.Send(request, ct);
}
