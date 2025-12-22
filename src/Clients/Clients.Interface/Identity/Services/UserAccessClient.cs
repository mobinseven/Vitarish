using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Vitarish.Clients.Interface.Identity.Services;

public class UserAccessClient(IPersonalClientFactory clientFactory, ILogger<UserAccessClient> logger) : IUserAccessClient
{
    private const string BaseUrl = "/api/users/access/";
    private Task<HttpClient> CreateAuthenticatedClientAsync() => clientFactory.CreateAuthenticatedClient(BaseUrl);
    public async Task<IEnumerable<UserDto>> GetUsers(CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = await CreateAuthenticatedClientAsync();
        string query = "";
        logger.LogInformation("Sending GET {query}", query);
        using HttpRequestMessage request = new(HttpMethod.Get, query);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<UserDto>>(cancellationToken: cancellationToken) ?? [];
    }
    public async Task<UserDto?> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = await CreateAuthenticatedClientAsync();
        string query = "current";
        logger.LogInformation("Sending GET {query}", query);
        using HttpRequestMessage request = new(HttpMethod.Get, query);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: cancellationToken);
    }

    public async Task<bool> CheckUserAccess(string resource, CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = await CreateAuthenticatedClientAsync();
        string query = "check";
        logger.LogInformation("Sending POST {query}", query);
        using HttpRequestMessage request = new(HttpMethod.Post, query)
        {
            Content = JsonContent.Create(new { Resource = resource })
        };
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
        {
            return true;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return false;
        }
        else
        {
            throw new InvalidDataException($"Either 204 or 202 is valid. status code: {response.StatusCode}");
        }
    }
}
