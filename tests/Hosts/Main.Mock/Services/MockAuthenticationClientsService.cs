using Vitarish.AuthenticationClients.Application.Abstractions;
using Vitarish.AuthenticationClients.Application.Commands;
using Vitarish.AuthenticationClients.Domain.Entities;

namespace Vitarish.Main.Mock.Services;

public sealed class MockAuthenticationClientsService : IAuthenticationClientsService
{
    private readonly List<AuthenticationClient> clients = [];

    public Task<AuthenticationClient?> GetAuthenticationClientAsync(string id, CancellationToken ct = default)
    {
        return Task.FromResult(clients.FirstOrDefault(client => client.Id == id));
    }

    public Task<IEnumerable<AuthenticationClient>> GetAuthenticationClientsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IEnumerable<AuthenticationClient>>(clients);
    }

    public Task<AuthenticationClient?> CreateAuthenticationClientAsync(
        CreateAuthenticationClient request,
        CancellationToken ct = default)
    {
        AuthenticationClient client = new()
        {
            Id = request.Id,
            Url = request.Url,
            Title = request.Title,
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            AutoAccess = request.AutoAccess
        };

        clients.Add(client);
        return Task.FromResult<AuthenticationClient?>(client);
    }

    public Task UpdateAuthenticationClientAsync(UpdateAuthenticationClient request, CancellationToken ct = default)
    {
        AuthenticationClient? client = clients.FirstOrDefault(item => item.Id == request.Id);
        if (client is null)
        {
            return Task.CompletedTask;
        }

        client.Url = request.Url;
        client.Title = request.Title;
        client.Description = request.Description;
        client.LogoUrl = request.LogoUrl;
        client.AutoAccess = request.AutoAccess;
        return Task.CompletedTask;
    }

    public Task DeleteAuthenticationClientAsync(DeleteAuthenticationClient request, CancellationToken ct = default)
    {
        _ = clients.RemoveAll(client => client.Id == request.Id);
        return Task.CompletedTask;
    }
}
