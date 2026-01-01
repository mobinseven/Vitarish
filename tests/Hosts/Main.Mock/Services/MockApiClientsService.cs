using Vitarish.ApiClients.Application.Abstractions;
using Vitarish.ApiClients.Application.Commands;
using Vitarish.ApiClients.Domain.Entities;

namespace Vitarish.Main.Mock.Services;

public sealed class MockApiClientsService : IApiClientsService
{
    private readonly List<ApiClient> clients = [];

    public Task<ApiClient?> GetApiClientAsync(string id, CancellationToken ct = default)
    {
        return Task.FromResult(clients.FirstOrDefault(client => client.Id == id));
    }

    public Task<IEnumerable<ApiClient>> GetApiClientsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<ApiClient>>(clients);
    }

    public Task<ApiClient?> CreateApiClientAsync(CreateApiClient request, CancellationToken ct = default)
    {
        ApiClient client = new()
        {
            Id = request.Id,
            Secret = request.Secret,
            Scopes = request.Scopes,
            HostUri = request.HostUri
        };
        clients.Add(client);
        return Task.FromResult<ApiClient?>(client);
    }

    public Task<ApiClient?> UpdateApiClientAsync(UpdateApiClient request, CancellationToken ct = default)
    {
        ApiClient? client = clients.FirstOrDefault(item => item.Id == request.Id);
        if (client is null)
        {
            return Task.FromResult<ApiClient?>(null);
        }

        client.Secret = request.Secret;
        client.Scopes = request.Scopes;
        client.HostUri = request.HostUri;
        return Task.FromResult<ApiClient?>(client);
    }

    public Task DeleteApiClientAsync(string id, CancellationToken ct = default)
    {
        _ = clients.RemoveAll(client => client.Id == id);
        return Task.CompletedTask;
    }
}
