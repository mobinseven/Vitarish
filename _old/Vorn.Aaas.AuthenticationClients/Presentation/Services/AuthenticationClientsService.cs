namespace Vorn.Aaas.AuthenticationClients.Presentation.Services;

using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vorn.Aaas.AuthenticationClients.Application.Commands;
using Vorn.Aaas.AuthenticationClients.Application.Queries;
using Vorn.Aaas.AuthenticationClients.Domain.Entities;

/// <summary>
/// Contract for working with AuthenticationClients inside the application (no HTTP required).
/// </summary>
public interface IAuthenticationClientsService
{
    Task<AuthenticationClient?> GetAuthenticationClientAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<AuthenticationClient>> GetAuthenticationClientsAsync(CancellationToken ct = default);
    /// <summary>
    /// Creates a client and returns its assigned id (mirrors the Created location of the HTTP API).
    /// </summary>
    Task<AuthenticationClient?> CreateAuthenticationClientAsync(CreateAuthenticationClientRequest request,
                                                                 CancellationToken ct = default);

    /// <summary>
    /// Updates an existing authentication client.
    /// </summary>
    Task UpdateAuthenticationClientAsync(UpdateAuthenticationClientRequest request,
                                         CancellationToken ct = default);

    /// <summary>
    /// Deletes an authentication client.
    /// </summary>
    Task DeleteAuthenticationClientAsync(DeleteAuthenticationClientRequest request,
                                         CancellationToken ct = default);
}

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
