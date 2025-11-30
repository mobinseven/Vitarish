
using MediatR;
using Vorn.Aaas.ApiClients.Application.Commands;
using Vorn.Aaas.ApiClients.Application.Queries;
using Vorn.Aaas.ApiClients.Domain.Entities;
using Vorn.Aaas.ApiClients.Presentation.Abstractions;
using Vorn.Aaas.ApiClients.Presentation.Contracts;

namespace Vorn.Aaas.ApiClients.Infrastructure.Services;
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

