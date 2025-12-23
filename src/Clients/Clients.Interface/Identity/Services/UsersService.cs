using System.Net.Http.Json;

namespace Vitarish.Clients.Interface.Identity.Services;

public class UsersService(IPersonalClientFactory clientFactory) : IUsersService
{
    private const string BaseUrl = "/api/users/";
    private Task<HttpClient> CreateAuthenticatedClientAsync() => clientFactory.CreateAuthenticatedClient(BaseUrl);
    public async Task<IEnumerable<UserDto>> GetUsersByRole(string roleName, CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = await CreateAuthenticatedClientAsync();
        string query = "role";
        using HttpRequestMessage request = new(HttpMethod.Post, query)
        {
            Content = JsonContent.Create(new { RoleName = roleName })
        };
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IEnumerable<UserDto>>(cancellationToken: cancellationToken) ?? [];
    }
    public async Task<bool> CheckUserRole(string roleName, CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = await CreateAuthenticatedClientAsync();
        string query = "checkrole";
        using HttpRequestMessage request = new(HttpMethod.Post, query)
        {
            Content = JsonContent.Create(new { RoleName = roleName })
        };
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        return response.StatusCode == System.Net.HttpStatusCode.OK;
    }
    public async Task<UserDto?> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = await CreateAuthenticatedClientAsync();
        string query = "current";
        using HttpRequestMessage request = new(HttpMethod.Get, query);
        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: cancellationToken);
    }
    public IEnumerable<UserDto> Users { get; private set; } = [];
    public UserDto? User { get; private set; }
    public virtual async Task Initialize(string[] roles)
    {
        List<UserDto> users = [];
        foreach (string role in roles)
        {
            users.AddRange(await GetUsersByRole(role));
        }
        Users = users.OrderBy(u => u.LastName);
        User = await GetCurrentUser();
    }
}

