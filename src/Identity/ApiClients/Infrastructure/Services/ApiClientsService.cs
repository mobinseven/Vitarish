
using MediatR;
using Vitarish.ApiClients.Application.Abstractions;
using Vitarish.ApiClients.Application.Commands;
using Vitarish.ApiClients.Application.Queries;
using Vitarish.ApiClients.Domain.Entities;

namespace Vitarish.ApiClients.Infrastructure.Services;
/// <inheritdoc />
public sealed class ApiClientsService(ISender sender) : IApiClientsService
{

    public Task<ApiClient?> GetApiClientAsync(string id, CancellationToken ct = default) =>
        sender.Send(new GetApiClient(id), ct);

    public Task<IEnumerable<ApiClient>> GetApiClientsAsync(CancellationToken ct = default) =>
        sender.Send(new GetApiClients(), ct);

    public async Task<ApiClient?> CreateApiClientAsync(CreateApiClient request, CancellationToken ct = default) =>
        await sender.Send(request, ct);

    public async Task<ApiClient?> UpdateApiClientAsync(UpdateApiClient request, CancellationToken ct = default) =>
        await sender.Send(request, ct);

    public Task DeleteApiClientAsync(string id, CancellationToken ct = default) =>
        sender.Send(new DeleteApiClient(id), ct);
}

