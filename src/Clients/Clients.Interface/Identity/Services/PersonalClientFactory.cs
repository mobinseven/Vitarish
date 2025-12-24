using Duende.IdentityModel.Client;
using Microsoft.Extensions.Options;
using Vitarish.Clients.Identity.Extensions;
using Vitarish.Clients.Identity.Options;
using Vitarish.Clients.Interface.Identity.Extensions;

namespace Vitarish.Clients.Interface.Identity.Services;

internal class PersonalClientFactory(IHttpClientFactory httpClientFactory,
    IPersonalAccessTokenClient personalAccessTokenClient,
    IIdentityService identityService,
    IOptions<IdentityClientOptions> options) : IPersonalClientFactory
{
    public async Task<HttpClient> CreateAuthenticatedClient(string? endpoint = null)
    {
        string userId = identityService.User?.GetId() ?? throw new InvalidOperationException("Can not create personal access token without a user.");
        string pat = await personalAccessTokenClient.CreateTokenAsync(userId) ?? throw new InvalidOperationException("Can not create an authenticated client without a valid token.");
        HttpClient httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(endpoint is null ? options.Value.Authority : UrlHelper.UrlCombine(options.Value.Authority, endpoint));
        httpClient.SetBearerToken(pat);
        return httpClient;
    }
}
