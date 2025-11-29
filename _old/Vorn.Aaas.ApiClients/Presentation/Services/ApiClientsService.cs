namespace Vorn.Aaas.ApiClients.Presentation.Services;

using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vorn.Aaas.ApiClients.Application.Commands;
using Vorn.Aaas.ApiClients.Application.Queries;
using Vorn.Aaas.ApiClients.Domain.Entities;

/// <summary>
/// Contract for working with ApiClients inside the application (no HTTP required).
/// </summary>
public interface IApiClientsService
{
    Task<ApiClient?> GetApiClientAsync(string id, CancellationToken ct = default);
    Task<IEnumerable<ApiClient>> GetApiClientsAsync(CancellationToken ct = default);
    /// <summary>
    /// Creates a new client and returns its assigned id (matches the CreatedLocation in the HTTP API).
    /// </summary>
    Task<ApiClient?> CreateApiClientAsync(CreateApiClientRequest request, CancellationToken ct = default);
    /// <summary>
    /// Edits an existing client and returns the updated entity (or null if not found).
    /// </summary>
    Task<ApiClient?> UpdateApiClientAsync(UpdateApiClientRequest request, CancellationToken ct = default);

    /// <summary>
    /// Removes a client by identifier.
    /// </summary>
    Task DeleteApiClientAsync(string id, CancellationToken ct = default);
}

/// <inheritdoc />
public sealed class ApiClientsService(ISender sender) : IApiClientsService
{

    public Task<ApiClient?> GetApiClientAsync(string id, CancellationToken ct = default) =>
        sender.Send(new GetApiClient(id), ct);

    public Task<IEnumerable<ApiClient>> GetApiClientsAsync(CancellationToken ct = default) =>
        sender.Send(new GetApiClients(), ct);

    public async Task<ApiClient?> CreateApiClientAsync(CreateApiClientRequest request, CancellationToken ct = default) =>
        await sender.Send(new CreateApiClient(request.Id, request.Secret, request.Scopes, request.HostUri), ct);

    public async Task<ApiClient?> UpdateApiClientAsync(UpdateApiClientRequest request, CancellationToken ct = default) =>
        await sender.Send(new UpdateApiClient(request.Id, request.Secret, request.Scopes, request.HostUri), ct);

    public Task DeleteApiClientAsync(string id, CancellationToken ct = default) =>
        sender.Send(new DeleteApiClient(id), ct);
}

